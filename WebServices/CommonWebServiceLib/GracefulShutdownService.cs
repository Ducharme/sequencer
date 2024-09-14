using log4net;

public class GracefulShutdownService : IHostedService
{
    private readonly IHostApplicationLifetime _appLifetime;
    private static readonly ILog logger = LogManager.GetLogger(typeof(GracefulShutdownService));

    public GracefulShutdownService(IHostApplicationLifetime appLifetime)
    {
        _appLifetime = appLifetime;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _appLifetime.ApplicationStopping.Register(OnShutdown);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async void OnShutdown()
    {
        logger.Info("Application is shutting down...");
        await CommonServiceLib.Program.Shutdown();
    }
}