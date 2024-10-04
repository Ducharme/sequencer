using log4net;

public enum Ec2InstanceAction
{
    Stop,
    Hibernate,
    Terminate,
    None
}

public class AwsEc2TerminationHandler : IHostedService, IDisposable
{
    private const string NotFound = "NotFound";
    private const string Unauthorized = "Unauthorized";
    private static readonly ILog logger = LogManager.GetLogger(typeof(AwsEc2TerminationHandler));
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly HttpClient _httpClient;
    private Timer? _timer;
    
    public AwsEc2TerminationHandler(IHostApplicationLifetime appLifetime)
    {
        _appLifetime = appLifetime;
        _httpClient = new HttpClient();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new Timer(CheckForTermination, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
        return Task.CompletedTask;
    }

    private bool IsStopping(string action)
    {
        var lowerAction = action.ToLower();
        return lowerAction == "stop" || lowerAction == "hibernate" || lowerAction == "terminate";
    }

    private async void CheckForTermination(object? state)
    {
        try
        {
            using var client = new HttpClient();
            var token = await AwsEc2MetadataReader.GetImdsv2Token(client);
            var spotMetadata = await AwsEc2MetadataReader.GetSpotMetadata(client, token);

            // Returns "stop" or "hibernate" or "terminate" when the instance is scheduled to go
            const string iaKey = AwsEc2MetadataReader.MetadataSpotInstanceActionKey;
            var ia = spotMetadata.ContainsKey(iaKey) ? spotMetadata[iaKey] : string.Empty;
            if (ia.Length > 0 && IsStopping(ia))
            {
                logger.Warn($"Spot instance {iaKey} is {ia}. Initiating graceful shutdown");
                await GracefulShutdown();
                return;
            }

            // Get termination time of spot instance
            const string ttKey = AwsEc2MetadataReader.MetadataSpotTerminationTimeKey;
            var tt = spotMetadata.ContainsKey(ttKey) ? spotMetadata[ttKey] : string.Empty;
            if (tt.Length > 0 && tt != NotFound && tt != Unauthorized)
            {
                logger.Warn($"Spot instance {ttKey} received is {tt}. Initiating graceful shutdown");
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