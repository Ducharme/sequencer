using log4net;

public static class AwsEnvironmentDetector
{
    private const string baseUrl = "http://169.254.169.254/latest";
    private const string metaUrl = $"{baseUrl}/meta-data";
    private const string tokenUrl = $"{baseUrl}/api/token";
    private static readonly ILog logger = LogManager.GetLogger(typeof(AwsEnvironmentDetector));
    
    public static bool IsRunningOnAWS()
    {
        return HasAwsEnvironmentVariables();
    }

    public static bool IsSpotInstance()
    {
        var ilc = AwsEc2MetadataReader.Ec2InstanceLifeCycleAsync().GetAwaiter().GetResult();
        return !string.IsNullOrEmpty(ilc) && ilc == "spot";
    }

    public static bool HasAwsEnvironmentVariables()
    {
        var ee = Environment.GetEnvironmentVariable("AWS_EXECUTION_ENV");
        var re = Environment.GetEnvironmentVariable("AWS_REGION");
        logger.Info($"AWS_EXECUTION_ENV={ee} and AWS_REGION={re}");
        return !string.IsNullOrEmpty(ee) || !string.IsNullOrEmpty(re);
    }
}