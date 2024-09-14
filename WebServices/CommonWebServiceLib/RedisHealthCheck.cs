using log4net;
using RedisAccessLayer;

public static class HealthEnpoints
{
    public static void MapGet(WebApplication app)
    {
        app.MapGet("/healthz", async (RedisPingHealthCheck healthCheck) =>
        {
            return await healthCheck.CheckAsync();
        });

        app.MapGet("/healthc", (RedisCachedHealthCheck healthCheck) =>
        {
            int statusCode = healthCheck.CheckSync();
            return Results.Text(statusCode.ToString());
        });

        app.MapGet("/healthd", (RedisDetailedHealthCheck healthCheck) =>
        {
            return healthCheck.CheckSync();
        });
    }
}

public abstract class RedisHealthCheckBase
{
    protected readonly IRedisConnectionManager _rcm;

    public RedisHealthCheckBase(IServiceProvider serviceProvider)
    {
        _rcm = serviceProvider.GetService<IRedisConnectionManager>() ?? throw new NullReferenceException("IRedisConnectionManager implementation could not be resolved");
    }
}

public class RedisCachedHealthCheck : RedisHealthCheckBase
{
    private static readonly ILog logger = LogManager.GetLogger(typeof(RedisPingHealthCheck));

    public RedisCachedHealthCheck(IServiceProvider serviceProvider) : base(serviceProvider) { }

    public int CheckSync()
    {
        try
        {
            var response = _rcm.GetHealthStatus();
            logger.Debug($"RedisConnectionManager GetHealthStatus returned {response}");
            return response;
        }
        catch (Exception ex)
        {
            logger.Error("Failed to GetHealthStatus from Redis", ex);
            return 503; // Service Unavailable
        }
    }
}

public class RedisPingHealthCheck : RedisHealthCheckBase
{
    private static readonly ILog logger = LogManager.GetLogger(typeof(RedisPingHealthCheck));

    public RedisPingHealthCheck(IServiceProvider serviceProvider) : base(serviceProvider) { }

    public async Task<int> CheckAsync()
    {
        try
        {
            var response = await _rcm.Ping();
            logger.Debug($"RedisConnectionManager Ping returned {response}");
            return response.TotalNanoseconds > 0 ? 200 : 500;
        }
        catch (Exception ex)
        {
            logger.Error("Failed to GetHealthStatus from Redis", ex);
            return 503; // Service Unavailable
        }
    }
}

public class RedisDetailedHealthCheck : RedisHealthCheckBase
{
    private static readonly ILog logger = LogManager.GetLogger(typeof(RedisDetailedHealthCheck));

    public RedisDetailedHealthCheck(IServiceProvider serviceProvider) : base(serviceProvider) { }

    public IResult CheckSync()
    {
        var code = _rcm.GetHealthStatus();
        // 200: OK "Healthy"
        // 500: Internal Server Error (having exceptions)
        // 503: Service Unavailable when not connected to redis
        IResult result;
        switch(code)
        {
            case StatusCodes.Status200OK: 
                var response = new { title = "OK", status = 200, detail = "Service is healthy" };
                result = Results.Json(response);
                break;
            case StatusCodes.Status500InternalServerError:
                result = Results.Problem("Redis client has errors", null, code, "Internal Server Error");
                break;
            case StatusCodes.Status503ServiceUnavailable:
                result = Results.Problem("Redis client is disconnected", null, code, "Service Unavailable");
                break;
            default:
                result = Results.Problem("Redis client returned unknown code", null, code, "Unknown code");
                break;
        }
        logger.Debug($"CheckHealthAsync is retuning {result}");
        return result;
    }
}