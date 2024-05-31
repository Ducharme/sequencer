
namespace RedisAccessLayer
{

    public interface IRedisConfigurationFetcher
    {
        IConfigurationOptionsWrapper OptionsWrapper { get; }
        string ClientName { get; }
    }
}
