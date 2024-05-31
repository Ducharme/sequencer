using StackExchange.Redis;

namespace RedisAccessLayer
{

    public class ConfigurationOptionsWrapper : IConfigurationOptionsWrapper
    {
        public ConfigurationOptions Options { get; }

        public ConfigurationOptionsWrapper(ConfigurationOptions co)
        {
            Options = co;
        }
    }
}
