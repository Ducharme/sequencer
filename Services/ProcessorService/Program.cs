using Microsoft.Extensions.DependencyInjection;
using log4net;

using CommonTypes;
using DatabaseAccessLayer;
using RedisAccessLayer;

namespace ProcessorService
{
    public static class Program
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(Program));
        
        public static ServiceProvider ConfigureServices(IServiceCollection services)
        {
            //bool isRunningInDocker = bool.TryParse(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), out bool runningInContainer) && runningInContainer;

            services.AddSingleton<IConnectionMultiplexerWrapper, ConnectionMultiplexerWrapper>();
            services.AddSingleton<IRedisConfigurationFetcher, EnvVarRedisConfigurationFetcher>();
            services.AddSingleton<IRedisConnectionManager, RedisConnectionManager>();
            services.AddSingleton<IDatabaseConnectionFetcher, EnvVarDatabaseConnectionFetcher>();
            // NOTE: Replace DatabaseDummyClient by DatabaseClient to use a real database
            services.AddSingleton<IDatabaseClient, DatabaseDummyClient>();
            services.AddSingleton<IPendingToProcessedListener, PendingListToProcessedListListener>();
            services.AddSingleton<ClientBase, PendingListToProcessedListListener>();
            services.AddSingleton<IProcessor, Processor>();
            var serviceProvider = services.BuildServiceProvider();
            return serviceProvider;
        }

        public async static Task<bool> Run(IServiceProvider serviceProvider)
        {
            var rcv = serviceProvider.GetService<IProcessor>() ?? throw new NullReferenceException("IProcessor implementation could not be resolved");
            await rcv.ReceiveMessageAsync();
            return true;
        }

        public static async Task Main(string[] args)
        {
            CommonServiceLib.Program.AssignEvents();
            CommonServiceLib.Program.ConfigureLogging();

            if (!EnvVarSetter.SetFromArgs(args, logger))
            {
                if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("REDIS_ENDPOINT")))
                    Environment.SetEnvironmentVariable("REDIS_ENDPOINT", "localhost");
                if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PGSQL_ENDPOINT")))
                    Environment.SetEnvironmentVariable("PGSQL_ENDPOINT", "localhost");
            }

            var serviceProvider = ConfigureServices(new ServiceCollection());
            CommonServiceLib.Program.AddServiceProvider(serviceProvider);
            object ret = await Run(serviceProvider);

            logger.Info($"Application exiting with value {ret}");
        }
    }
}
