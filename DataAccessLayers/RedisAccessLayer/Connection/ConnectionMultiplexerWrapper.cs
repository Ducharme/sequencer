using CommonTypes;
using log4net;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace RedisAccessLayer
{
    public class ConnectionMultiplexerWrapper : IConnectionMultiplexerWrapper
    {
        public IConnectionMultiplexer Connect(IConfigurationOptionsWrapper cow, ILog logger)
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddFilter("Microsoft", LogLevel.Warning)
                    .AddFilter("System", LogLevel.Warning)
                    .AddFilter("StackExchange.Redis", LogLevel.Trace)
                    .AddProvider(new Log4NetProvider(logger));
            });

            var redisLogger = loggerFactory.CreateLogger("Redis");
            var loggerTextWriter = new LoggerTextWriter(redisLogger);
            return ConnectionMultiplexer.Connect(cow.Options, loggerTextWriter);
        }
    }
}