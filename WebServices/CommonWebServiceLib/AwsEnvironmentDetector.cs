using log4net;

public static class AwsEnvironmentDetector
{
    private static readonly ILog logger = LogManager.GetLogger(typeof(AwsEnvironmentDetector));
    
    public static bool IsRunningOnAWS()
    {
        return HasAwsEnvironmentVariables() || IsEC2InstanceAsync().GetAwaiter().GetResult();
    }

    private static bool HasAwsEnvironmentVariables()
    {
        var ee = Environment.GetEnvironmentVariable("AWS_EXECUTION_ENV");
        var re = Environment.GetEnvironmentVariable("AWS_REGION");
        logger.Info($"AWS_EXECUTION_ENV={ee} and AWS_REGION={re}");
        return !string.IsNullOrEmpty(ee) || !string.IsNullOrEmpty(re);
    }

    private static async Task<bool> IsEC2InstanceAsync()
    {
        bool success;
        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(1);
            var response = await client.GetAsync("http://169.254.169.254/latest/meta-data/");
            success = response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            logger.Info($"Failed to check EC2 meta-data. Source={ex.Source} Message={ex.Message}");
            success = false;
        }
        return success;
    }
}