using Microsoft.AspNetCore.Mvc;
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
var app = builder.Build();

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


var adm = app.Services.GetService<AS.IAdminManager>() ?? throw new NullReferenceException("IAdminManager implementation could not be resolved");

app.MapGet("/", () => "Hello World!");

HealthEnpoints.MapGet(app);

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
    var lst = await adm.GetAllMessagesFromPendingStream(name);
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

        var stats = new Stats(filtered);
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
});

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

        var perfs = new Perfs(filtered);
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
});

app.MapGet("/redis/infos", async () => {
    var res = await adm.RedisServerInfos();
    return Results.Json(new { res });
});

app.Run();
