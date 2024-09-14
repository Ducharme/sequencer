using Microsoft.Extensions.Hosting;
using log4net;

public class AwsEc2SpotTerminationHandler : IHostedService, IDisposable
{
    private const string url = "http://169.254.169.254/latest/meta-data/spot/termination-time";
    private static readonly ILog logger = LogManager.GetLogger(typeof(AwsEc2SpotTerminationHandler));
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly HttpClient _httpClient;
    private Timer? _timer;

    public AwsEc2SpotTerminationHandler(IHostApplicationLifetime appLifetime)
    {
        _appLifetime = appLifetime;
        _httpClient = new HttpClient();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new Timer(CheckForTermination, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
        return Task.CompletedTask;
    }

    private async void CheckForTermination(object? state)
    {
        try
        {
            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                logger.Warn("Spot instance termination notice received. Initiating graceful shutdown");
                await GracefulShutdown();
            }
        }
        catch (Exception ex)
        {
            logger.Error("Error checking for spot termination", ex);
        }
    }

    private async Task GracefulShutdown()
    {
        // Perform any cleanup or state saving operations here
        logger.Info("Performing graceful shutdown tasks...");
        
        await CommonServiceLib.Program.Shutdown();
        
        // Stop accepting new requests
        _appLifetime.StopApplication();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
        _httpClient.Dispose();
    }
}