using Microsoft.AspNetCore.Mvc;
//using System.Runtime;
//using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Http.Timeouts;

using log4net;

using CommonTypes;
using RedisAccessLayer;
using AS=AdminService;

CommonServiceLib.Program.AssignEvents();
// Consider https://aspdotnethelp.com/implement-log4net-in-asp-net-core-application/
ILog logger = LogManager.GetLogger(typeof(Program));
CommonServiceLib.Program.ConfigureLogging();

var builder = WebApplication.CreateBuilder(args ?? []);
//builder.Logging.AddProvider(new Log4NetProvider(logger));
var serviceProvider = AS.Program.ConfigureServices(builder.Services);
CommonServiceLib.Program.AddServiceProvider(serviceProvider);

builder.Services.AddRazorPages();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<RedisCachedHealthCheck>();
builder.Services.AddSingleton<RedisDetailedHealthCheck>();
builder.Services.AddSingleton<RedisPingHealthCheck>();
builder.Services.AddHostedService<GracefulShutdownService>();
if (AwsEnvironmentDetector.IsRunningOnAWS() && AwsEnvironmentDetector.IsSpotInstance())
{
    builder.Services.AddHostedService<AwsEc2TerminationHandler>();
}
//builder.Services.Configure<KestrelServerOptions>(options =>
//{
//    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(10);
//    options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(10);
//});
builder.WebHost.ConfigureKestrel(options =>
{
    options.AllowSynchronousIO = false;
    options.Limits.MinRequestBodyDataRate = null;
    options.Limits.MinResponseDataRate = null;
    //options.Limits.MaxRequestBodySize = 100_000_000; // 100MB
    //options.Limits.MaxRequestBufferSize = 100_000_000;
});
// For larger memory usage:
//GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
builder.Services.AddRequestTimeouts(options => {
    options.AddPolicy("LongOperationPolicy", new RequestTimeoutPolicy {
        Timeout = TimeSpan.FromMinutes(10),
        TimeoutStatusCode = 498
    });
});

var app = builder.Build();
app.UseRequestTimeouts();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
    app.UseSwagger();
    app.UseSwaggerUI();
}

//TODO: Uncomment app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapRazorPages();
HealthEnpoints.MapGet(app);
ThreadPool.SetMinThreads(20, 20);
CommonServiceLib.Program.LogNumberOfThreads();

var adm = app.Services.GetService<AS.IAdminManager>() ?? throw new NullReferenceException("IAdminManager implementation could not be resolved");

app.MapGet("/", () => "Hello World!");

app.MapGet("/index.htm", () => {
var htmlContent = @"<!doctype html>
<html>
  <head>
    <title>My Simple HTML Page</title>
  </head>
  <body>
    <h1>Hello World</h1>
    <p>This is a simple HTML page.</p>
  </body>
</html>
";

    return Results.Text(htmlContent, "text/html", System.Text.Encoding.UTF8, 200);
});

app.MapDelete("/list/sequencer/messages", async (string name) => {
    var rlm = app.Services.GetService<IListStreamAdminClient>() ?? throw new NullReferenceException("IListStreamAdminClient implementation could not be resolved");
    await adm.DeleteLists(string.Empty);
    return Results.Ok(name);
});

app.MapDelete("/list/sequenced/messages", async (string name) => {
    var rlm = app.Services.GetService<IListStreamAdminClient>() ?? throw new NullReferenceException("IListStreamAdminClient implementation could not be resolved");
    await adm.DeleteLists(name);
    return Results.Ok(name);
});

app.MapGet("/list/sequenced/count", async (string name) => {
    var rlm = app.Services.GetService<IListStreamAdminClient>() ?? throw new NullReferenceException("IListStreamAdminClient implementation could not be resolved");
    var count = await adm.GetSequencedListMessagesCount(name);
    return Results.Text(count.ToString(), "text/html", System.Text.Encoding.UTF8, 200);
});

app.MapGet("/stream/sequenced/count", async (string name) => {
    var rlm = app.Services.GetService<IListStreamAdminClient>() ?? throw new NullReferenceException("IListStreamAdminClient implementation could not be resolved");
    var count = await adm.GetSequencedStreamMessagesCount(name);
    return Results.Text(count.ToString(), "text/html", System.Text.Encoding.UTF8, 200);
});

app.MapGet("/stream/sequenced/last", async (string name) => {
    var rlm = app.Services.GetService<IListStreamAdminClient>() ?? throw new NullReferenceException("IListStreamAdminClient implementation could not be resolved");
    var msg = await adm.GetSequencedStreamLastMessage(name);
    return msg == null
        ? Results.Json(new {}, statusCode: StatusCodes.Status200OK, contentType: "application/json")
        : Results.Json(msg, statusCode: StatusCodes.Status200OK, contentType: "application/json");
});

app.MapPost("/list/sequencer/messages", async (GenerateMessagesRequest request) => {
    return await GenerateMessages(request, "/list/sequencer/messages");
});

app.MapDelete("/messages", async (string name) => {
    logger.Info($"Called DELETE /messages with name={name}");
    var rlm = app.Services.GetService<IListStreamAdminClient>() ?? throw new NullReferenceException("IListStreamAdminClient implementation could not be resolved");
    await adm.PrepareDatabase(name);
    await adm.DeleteStreams(name);
    await adm.DeleteLists(name);
    return Results.Ok(name);
});

app.MapPost("/messages", async (GenerateMessagesRequest request)  => {
    return await GenerateMessages(request, "/messages");
});

async Task<IResult> GenerateMessages(GenerateMessagesRequest request, string endpoint)
{
    if (request == null)
    {
        return Results.BadRequest("Request is null on " + endpoint);
    }

    string name = request.Name ?? string.Empty;
    int numberOfMessages = request.NumberOfMessages;
    int creationDelay = request.CreationDelay;
    int processingTime = request.ProcessingTime;

    logger.Info($"Called POST {endpoint} with name={name}, numberOfMessages={numberOfMessages}, creationDelay={creationDelay}, processingTime={processingTime}");
    var rlm = app.Services.GetService<IListStreamAdminClient>() ?? throw new NullReferenceException("IListStreamAdminClient implementation could not be resolved");

    var ret = await adm.GeneratePendingList(name, numberOfMessages, creationDelay, processingTime);
    if (ret)
    {
        if (creationDelay == 0)
        {
            return Results.Created(endpoint, name);
        }
        else
        {
            return Results.Accepted(endpoint, name);
        }
    }
    else
    {
        return Results.BadRequest(endpoint);
    }
}

app.MapGet("/list/pending/messages", async (string name) => {
    logger.Info($"Called /list/pending/messages with name={name}");
    var rlm = app.Services.GetService<IListStreamAdminClient>() ?? throw new NullReferenceException("IListStreamAdminClient implementation could not be resolved");
    var lst = await adm.GetAllMessagesFromPendingList(name);
    return Results.Json(lst);
});

app.MapGet("/list/processing/messages", async (string name) => {
    logger.Info($"Called /list/processing/messages with name={name}");
    var rlm = app.Services.GetService<IListStreamAdminClient>() ?? throw new NullReferenceException("IListStreamAdminClient implementation could not be resolved");
    var lst = await adm.GetAllMessagesFromProcessingList(name);
    return Results.Json(lst);
});

app.MapGet("/list/processed/messages", async (string name) => {
    logger.Info($"Called /list/processed/messages with name={name}");
    var rlm = app.Services.GetService<IListStreamAdminClient>() ?? throw new NullReferenceException("IListStreamAdminClient implementation could not be resolved");
    var lst = await adm.GetAllMessagesFromSequencedList(name);
    return Results.Json(lst);
});

app.MapGet("/stream/pending/messages", async (string name) => {
    logger.Info($"Called /stream/pending/messages with name={name}");
    var lst = await adm.GetAllMessagesFromSequencedStream(name);
    return Results.Json(lst);
});

app.MapGet("/stream/processed/messages", async (string name) => {
    logger.Info($"Called /stream/processed/messages with name={name}");
    var lst = await adm.GetAllMessagesFromProcessedStream(name);
    return Results.Json(lst);
});

app.MapGet("/list/stats", async ([FromQuery] string name, [FromQuery] long start = 1, [FromQuery] long count = -1) => {
    logger.Info($"Called /list/stats with name={name}, start={start}, count={count}");

    try
    {
        var res = new MessagesFromSequencedListResult(name, start, count);
        if (res.Result != null)
        {
            return Results.Json(res.Result);
        }

        var filtered = await res.Fetch(adm);
        if (res.Result != null)
        {
            return Results.Json(res.Result);
        }

        var stats = await Stats.CreateAsync(filtered);
        var check = new OrderingCheck(filtered);
        var obj = new { start, filtered.Count, stats, check };
        return Results.Json(obj);
    }
    catch (Exception ex)
    {
        logger.Error($"Failed /list/stats with name={name}, start={start}, count={count}", ex);
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An error occurred",
            Detail = ex.Message
        };
        return Results.Problem(problemDetails);
    }
}).WithRequestTimeout("LongOperationPolicy");

app.MapGet("/list/perfs", async ([FromQuery] string name, [FromQuery] long start = 1, [FromQuery] long count = -1) => {
    logger.Info($"Called /list/perfs with name={name}, start={start}, count={count}");

    try
    {
        var res = new MessagesFromSequencedListResult(name, start, count);
        if (res.Result != null)
        {
            return Results.Json(res.Result);
        }

        var filtered = await res.Fetch(adm);
        if (res.Result != null)
        {
            return Results.Json(res.Result);
        }

        var perfs = await Perfs.CreateAsync(filtered);
        var obj = new { start, filtered.Count, perfs };
        return Results.Json(obj);
    }
    catch (Exception ex)
    {
        logger.Error($"Failed /list/perfs with name={name}, start={start}, count={count}", ex);
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An error occurred",
            Detail = ex.Message
        };
        return Results.Problem(problemDetails);
    }
}).WithRequestTimeout("LongOperationPolicy");

app.MapDelete("/list/cache", () => {
    logger.Info($"Called DELETE /list/cache");
    {
        adm.ClearCache();
        return Results.Ok();
    }
});

app.MapGet("/redis/infos", async () => {
    var res = await adm.RedisServerInfos();
    return Results.Json(new { res });
});

app.MapGet("/dump", async ([FromQuery] string name) => {

    var pendingList = await adm.GetAllMessagesFromPendingList(name);
    var processingList = await adm.GetAllMessagesFromProcessingList(name);
    var processedStream = await adm.GetAllMessagesFromProcessedStream(name);
    var pendingStream = await adm.GetAllMessagesFromSequencedStream(name);
    var sequencedList = await adm.GetAllMessagesFromSequencedList(name);

    var pendingListMin = pendingList.Count > 0 ? pendingList.Min(m => m.Sequence) : 1;
    var processingListMin = processingList.Count > 0 ? processingList.Min(m => m.Sequence) : 1;
    var processedStreamMin = processedStream.Count > 0 ? processedStream.Min(m => m.Sequence) : 1;
    var pendingStreamMin = pendingStream.Count > 0 ? pendingStream.Min(m => m.Sequence) : 1;
    var sequencedListMin = sequencedList.Count > 0 ? sequencedList.Min(m => m.Sequence) : 1;
    var minArr = new [] { pendingListMin, processingListMin, processedStreamMin, pendingStreamMin, sequencedListMin };
    var min = minArr.Min();

    var pendingListMax = pendingList.Count > 0 ? pendingList.Max(m => m.Sequence) : 0;
    var processingListMax = processingList.Count > 0 ? processingList.Max(m => m.Sequence) : 0;
    var processedStreamMax = processedStream.Count > 0 ? processedStream.Max(m => m.Sequence) : 0;
    var pendingStreamMax = pendingStream.Count > 0 ? pendingStream.Max(m => m.Sequence) : 0;
    var sequencedListMax = sequencedList.Count > 0 ? sequencedList.Max(m => m.Sequence) : 0;
    var maxArr = new [] { pendingListMax, processingListMax, processedStreamMax, pendingStreamMax, sequencedListMax };
    var max = maxArr.Max();

    var html = "<html><head><title>Dump</title><head><body><table border=\"1\">\n";
    html += "<tr><th>#</th><th>Source</th><th>Sequence</th><th>CreatedAt</th><th>ProcessingAt</th><th>ProcessedAt</th><th>SequencingAt</th><th>SavedAt</th><th>SequencedAt</th><th>PendingStreamId</th></tr>";
    for (var i=min; i <= max; i++)
    {
        var pendingListMsg = pendingList.FirstOrDefault(m => m.Sequence == i);
        var processingListMsg = processingList.FirstOrDefault(m => m.Sequence == i);
        var processedStreamMsg = processedStream.FirstOrDefault(m => m.Sequence == i);
        var pendingStreamMsg = pendingStream.FirstOrDefault(m => m.Sequence == i);
        var sequencedListMsg = sequencedList.FirstOrDefault(m => m.Sequence == i);

        var processedStreamIds = processedStream.Select(m => m.StreamId).Order().ToList();
        var pendingStreamIds = pendingStream.Select(m => m.StreamId).Order().ToList();

        var latest = sequencedListMsg ?? pendingStreamMsg ?? processedStreamMsg ?? processingListMsg ?? pendingListMsg;
        var source = latest == sequencedListMsg ? "sequencedList" : (latest == pendingStreamMsg ? "pendingStream" : (latest == processedStreamMsg ? "processedStream" : (latest == processingListMsg ? "processingList" : (latest == pendingListMsg ? "pendingList" : string.Empty))));
        var get3Decimals = (string s) => { var decimals = s.IndexOf('.'); if (decimals > 0) { return s.PadRight(decimals + 4); } else { return s + ".000"; } };
        var tc = (long? dt) => dt == null ? string.Empty : get3Decimals(MyMessage.GetTimeAsString(dt.Value));
        html += $"<tr><td>{i}</td><th>{source}</th><td>{latest?.Sequence}</td><td>{tc(latest?.CreatedAt)}</td><td>{latest?.ProcessingAt-latest?.CreatedAt}</td><td>{latest?.ProcessedAt-latest?.CreatedAt}</td>";
        html += $"<td>{latest?.SequencingAt-latest?.CreatedAt}</td><td>{latest?.SavedAt-latest?.CreatedAt}</td><td>{latest?.SequencedAt-latest?.CreatedAt}</td>";
        var processedStreamInfo = processedStreamMsg == null ? string.Empty : $"{processedStreamMsg.StreamId} ({processedStreamIds.IndexOf(processedStreamMsg.StreamId)})";
        var pendingStreamInfo = pendingStreamMsg == null ? string.Empty : $"{pendingStreamMsg.StreamId} ({pendingStreamIds.IndexOf(pendingStreamMsg.StreamId)})";
        html += $"<td>{pendingStreamInfo}</td></tr>\n";
    }
    html += "</table></body></html>";

    return Results.Content(html, "text/html", System.Text.Encoding.UTF8, 200);
}).WithRequestTimeout("LongOperationPolicy");

app.Run();
