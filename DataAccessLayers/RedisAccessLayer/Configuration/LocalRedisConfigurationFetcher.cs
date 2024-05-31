
namespace RedisAccessLayer
{
    public class LocalRedisConfigurationFetcher : RedisConfigurationFetcherBase
    {
        public LocalRedisConfigurationFetcher()
            : base("localhost")
        {
            var co = GetConfigurationOptions();
            OptionsWrapper = new ConfigurationOptionsWrapper(co);
        }
    }
}
