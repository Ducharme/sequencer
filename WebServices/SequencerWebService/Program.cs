using log4net;
using SS=SequencerService;

CommonServiceLib.Program.AssignEvents();
ILog logger = LogManager.GetLogger(typeof(Program));
CommonServiceLib.Program.ConfigureLogging();

var builder = WebApplication.CreateBuilder(args);
var serviceProvider = SS.Program.ConfigureServices(builder.Services);
CommonServiceLib.Program.AddServiceProvider(serviceProvider);

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<RedisCachedHealthCheck>();
builder.Services.AddSingleton<RedisDetailedHealthCheck>();
builder.Services.AddSingleton<RedisPingHealthCheck>();
builder.Services.AddHostedService<SequencerHostedService>();
builder.Services.AddHostedService<GracefulShutdownService>();
if (AwsEnvironmentDetector.IsRunningOnAWS())
{
    builder.Services.AddHostedService<AwsEc2SpotTerminationHandler>();
}
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

HealthEnpoints.MapGet(app);

app.Run();
