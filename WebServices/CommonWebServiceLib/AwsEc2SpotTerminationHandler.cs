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
    private const string baseUrl = "http://169.254.169.254/latest";
    private const string metaUrl = $"{baseUrl}/meta-data";
    private const string toeknUrl = $"{baseUrl}/api/token";
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

    private static async Task<string> GetImdsv2Token(HttpClient client)
    {
        client.DefaultRequestHeaders.Add("X-aws-ec2-metadata-token-ttl-seconds", "21600");
        var response = await client.PutAsync(toeknUrl, null);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    private static async Task<string> GetMetadata(HttpClient client, string token, string path)
    {
        try
        {
            client.DefaultRequestHeaders.Add("X-aws-ec2-metadata-token", token);
            var response = await client.GetAsync($"{metaUrl}/{path}");
            return response.IsSuccessStatusCode ? await response.Content.ReadAsStringAsync() : response.StatusCode.ToString();
        }
        catch (HttpRequestException)
        {
            return string.Empty; // No action scheduled
        }
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
            var token = await GetImdsv2Token(client);

            // Returns "stop" or "hibernate" or "terminate" when the instance is scheduled to go
             var spotInstanceAction = await GetMetadata(client, token, "spot/instance-action");
            logger.Info($"spot/instance-action is {spotInstanceAction}");
            if (IsStopping(spotInstanceAction))
            {
                logger.Warn("Spot instance termination notice received. Initiating graceful shutdown");
                await GracefulShutdown();
                return;
            }

            // Get termination time of sot instance
            var spotTerminationTime = await GetMetadata(client, token, "spot/termination-time");
            logger.Info($"spot/termination-time is {spotTerminationTime}");
            if (spotTerminationTime.Length > 0)
            {
                logger.Warn("Spot instance termination time received. Initiating graceful shutdown");
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