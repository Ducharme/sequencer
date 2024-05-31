using StackExchange.Redis;

namespace RedisAccessLayer
{
    public interface IConnectionMultiplexerWrapper
    {
        IConnectionMultiplexer Connect(IConfigurationOptionsWrapper cow);
    }
}