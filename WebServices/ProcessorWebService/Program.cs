using log4net;
using PS=ProcessorService;

PS.Program.AssignEvents();
ILog logger = LogManager.GetLogger(typeof(Program));
PS.Program.ConfigureLogging();

var builder = WebApplication.CreateBuilder(args);
var serviceProvider = PS.Program.ConfigureServices(builder.Services);

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<RedisCachedHealthCheck>();
builder.Services.AddSingleton<RedisDetailedHealthCheck>();
builder.Services.AddSingleton<RedisPingHealthCheck>();
builder.Services.AddHostedService<ProcessorHostedService>();
builder.Services.AddHostedService<GracefulShutdownService>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseHsts();
}

//TODO: Uncomment app.UseHttpsRedirection();
app.UseRouting();
//app.UseAuthorization();

app.MapGet("/healthz", async (RedisPingHealthCheck healthCheck) =>
{
    return await healthCheck.CheckAsync();
});

app.MapGet("/healthc", (RedisCachedHealthCheck healthCheck) =>
{
    int statusCode = healthCheck.CheckSync();
    return Results.Text(statusCode.ToString());
});

app.MapGet("/healthd", (RedisDetailedHealthCheck healthCheck) =>
{
    return healthCheck.CheckSync();
});

app.Run();
