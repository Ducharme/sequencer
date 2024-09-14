using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class SpotTerminationHandler : IHostedService, IDisposable
{
    private const string url = "http://169.254.169.254/latest/meta-data/spot/termination-time";
    private readonly ILogger<SpotTerminationHandler> _logger;
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly HttpClient _httpClient;
    private Timer? _timer;

    public SpotTerminationHandler(ILogger<SpotTerminationHandler> logger, IHostApplicationLifetime appLifetime)
    {
        _logger = logger;
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
                _logger.LogWarning("Spot instance termination notice received. Initiating graceful shutdown.");
                await GracefulShutdown();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for spot termination.");
        }
    }

    private async Task GracefulShutdown()
    {
        // Perform any cleanup or state saving operations here
        _logger.LogInformation("Performing graceful shutdown tasks...");
        
        await AdminService.Program.Shutdown();
        
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