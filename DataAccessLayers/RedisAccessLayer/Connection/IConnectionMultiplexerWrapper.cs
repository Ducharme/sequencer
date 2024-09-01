using log4net;
using StackExchange.Redis;

namespace RedisAccessLayer
{
    public interface IConnectionMultiplexerWrapper
    {
        IConnectionMultiplexer Connect(IConfigurationOptionsWrapper cow, ILog logger);
    }
}