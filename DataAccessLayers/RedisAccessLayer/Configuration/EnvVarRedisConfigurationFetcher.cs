
using CommonTypes;

using log4net;
using StackExchange.Redis;

namespace RedisAccessLayer
{
    public class EnvVarRedisConfigurationFetcher : RedisConfigurationFetcherBase
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(EnvVarRedisConfigurationFetcher));

        public EnvVarRedisConfigurationFetcher()
            : base(EnvVarReader.GetString("REDIS_ENDPOINT", EnvVarReader.NotFound), EnvVarReader.GetInt("REDIS_PORT", DefaultPort))
        {
            var co = GetConfigurationOptions();
            
            var user = EnvVarReader.GetString("REDIS_USER", EnvVarReader.NotFound);
            var password = EnvVarReader.GetString("REDIS_PASSWORD", EnvVarReader.NotFound);
            var sslEnabled = EnvVarReader.GetString("REDIS_SSL_ENABLED", EnvVarReader.NotFound);
            
            var sslProtocols = EnvVarReader.GetString("REDIS_SSL_PROTOCOLS", EnvVarReader.NotFound);
 
            logger.Info($"REDIS_ENDPOINT is {cacheEndpoint}");
            logger.Info($"REDIS_PORT is {cachePort}");

            if (user != EnvVarReader.NotFound)
            {
                logger.Info($"REDIS_USER is {user}");
                co.User = user;
            }

            if (password != EnvVarReader.NotFound)
            {
                logger.Info($"REDIS_PASSWORD is set");
                co.Password = password;
            }

            if (sslEnabled != EnvVarReader.NotFound)
            {
                logger.Info($"REDIS_SSL_ENABLED is {sslEnabled}");
                co.Ssl = EnvVarReader.GetBool("REDIS_SSL_ENABLED", false);
            }

            if (sslProtocols != EnvVarReader.NotFound && !string.IsNullOrEmpty(sslProtocols))
            {
                logger.Info($"REDIS_SSL_PROTOCOLS is {sslProtocols}");
                if (Enum.TryParse(sslProtocols, true, out System.Security.Authentication.SslProtocols parsedSslProtocols))
                {
                    co.SslProtocols = parsedSslProtocols;
                }
                else
                {
                    var type = typeof(System.Security.Authentication.SslProtocols);
                    var sslProtocolsNames = Enum.GetNames(type);
                    var validValues = string.Join(", ", sslProtocolsNames);
                    logger.Error($"Invalid value for REDIS_SSL_PROTOCOLS: {sslProtocols} (valid values are {validValues})");
                }
            }

            var channelPrefix = EnvVarReader.GetString("REDIS_CHANNEL_PREFIX", EnvVarReader.NotFound);
            if (channelPrefix != EnvVarReader.NotFound)
            {
                logger.Info($"REDIS_CHANNEL_PREFIX is {channelPrefix}");
                co.ChannelPrefix = RedisChannel.Literal(channelPrefix);
            }

            var useCommandMapStr = EnvVarReader.GetString("REDIS_USE_COMMAND_MAP", EnvVarReader.NotFound);
            if (useCommandMapStr != EnvVarReader.NotFound)
            {
                logger.Info($"REDIS_USE_COMMAND_MAP is {useCommandMapStr}");
                var useCommandMap = EnvVarReader.GetBool("REDIS_USE_COMMAND_MAP", false);
                if (useCommandMap)
                {
                    var dic = new Dictionary<string, string?>
                    {
                        { "WATCH", null },
                        { "UNWATCH", null }
                    };
                    co.CommandMap = CommandMap.Create(dic);
                }
            }

            OptionsWrapper = new ConfigurationOptionsWrapper(co);
        }
    }
}