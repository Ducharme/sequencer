using log4net;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RedisAccessLayer;

public class RedisHealthCheck : IHealthCheck
{
    private readonly IServiceProvider _serviceProvider;
    private static readonly ILog logger = LogManager.GetLogger(typeof(RedisHealthCheck));

    public RedisHealthCheck(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var rlm = _serviceProvider.GetService<IListStreamAdminClient>() ?? throw new NullReferenceException("IListStreamAdminClient implementation could not be resolved");
            var response = await rlm.Ping();
            return HealthCheckResult.Healthy("A healthy result.");
        }
        catch (Exception ex)
        {
            logger.Error("Failed to ping", ex);
            return HealthCheckResult.Unhealthy("An unhealthy result.");
        }
    }
}