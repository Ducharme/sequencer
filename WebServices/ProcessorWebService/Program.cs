using log4net;
using PS=ProcessorService;

ILog logger = LogManager.GetLogger(typeof(Program));
PS.Program.ConfigureLogging();

var builder = WebApplication.CreateBuilder(args);
var serviceProvider = PS.Program.ConfigureServices(builder.Services);

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//TODO: Uncomment app.UseHttpsRedirection();

// Because next call is not awaited, execution of the method continues before the call is completed
#pragma warning disable CS4014
PS.Program.Run(serviceProvider);
#pragma warning restore CS4014

app.MapGet("/healthz", () =>
{
    bool isHealthy = true; // TODO: Perform health checks here
    return isHealthy ? Results.Ok("Healthy") : Results.StatusCode(StatusCodes.Status500InternalServerError);
});

app.Run();
