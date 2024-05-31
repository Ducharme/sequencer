using System.Diagnostics;
using CommonTypes;
using log4net;
using StackExchange.Redis;

namespace RedisAccessLayer
{
    public abstract class RedisConfigurationFetcherBase : IRedisConfigurationFetcher
    {
        public IConfigurationOptionsWrapper OptionsWrapper { get; protected set; }
        protected readonly string cacheEndpoint;
        protected readonly int cachePort;
        protected readonly string clientName;
        protected const string DefaultEndpoint = "localhost";
        protected const int DefaultPort = 6379;
        protected const int DefaultConnectTimeout = 5000;
        protected const int DefaultSyncTimeout = 5000;
        protected const int DefaulExponentialRetryDelta = 100;
        protected const int DefaulExponentialRetryMax = 10000;
        public string ClientName { get { return clientName; } }
        private static readonly ILog logger = LogManager.GetLogger(typeof(RedisConfigurationFetcherBase));

        public RedisConfigurationFetcherBase(string enpoint = DefaultEndpoint, int port = DefaultPort)
        {
            cacheEndpoint = enpoint;
            cachePort = port;

            var hostName = System.Net.Dns.GetHostName();
            var hn = !string.IsNullOrEmpty(hostName) ? hostName : string.Empty;

            var machineName = Environment.MachineName;
            var mn = !string.IsNullOrEmpty(machineName) ? machineName : string.Empty;
            
            var name = hn.Length > 0 ? hn : (mn.Length > 0 ? mn : GetRandomString());
            var processId = Process.GetCurrentProcess().Id;
            clientName = $"seq-client-{name}-{processId}";
            logger.Info($"RedisConfigurationFetcherBase created for {clientName}");

            OptionsWrapper = new ConfigurationOptionsWrapper(new ConfigurationOptions {});
        }

        protected ConfigurationOptions GetConfigurationOptions()
        {
            var co = new ConfigurationOptions
            {
                EndPoints = { $"{cacheEndpoint}:{cachePort}" },
                AbortOnConnectFail = false,
                ConnectTimeout = DefaultConnectTimeout,
                SyncTimeout = DefaultSyncTimeout,
                ReconnectRetryPolicy = new ExponentialRetry(DefaulExponentialRetryDelta, DefaulExponentialRetryMax),
                ClientName = clientName
            };

            return co;
        }

        public override string ToString()
        {
            return $"Server={cacheEndpoint};Port={cachePort};";
        }

        private const int RandomStringLength = 8;
        private const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        private static string GetRandomString()
        {
            var random = new Random();
            var randomStr = new string(Enumerable.Repeat(Chars, RandomStringLength)
                .Select(s => s[random.Next(s.Length)]).ToArray());
            return randomStr;
        }
    }
}