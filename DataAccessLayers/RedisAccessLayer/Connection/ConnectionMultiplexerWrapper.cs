using StackExchange.Redis;

namespace RedisAccessLayer
{
    public class ConnectionMultiplexerWrapper : IConnectionMultiplexerWrapper
    {
        public IConnectionMultiplexer Connect(IConfigurationOptionsWrapper cow)
        {
            return ConnectionMultiplexer.Connect(cow.Options);
        }
    }
}