using StackExchange.Redis;

namespace RedisAccessLayer
{
    public interface IConfigurationOptionsWrapper
    {
        ConfigurationOptions Options { get; }
    }
}
