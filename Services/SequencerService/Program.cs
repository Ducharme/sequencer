using Microsoft.Extensions.DependencyInjection;
using log4net;

using CommonTypes;
using DatabaseAccessLayer;
using RedisAccessLayer;

namespace SequencerService
{
    public static class Program
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(Program));

        public static ServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IConnectionMultiplexerWrapper, ConnectionMultiplexerWrapper>();
            services.AddSingleton<IRedisConfigurationFetcher, EnvVarRedisConfigurationFetcher>();
            services.AddSingleton<IRedisConnectionManager, RedisConnectionManager>();
            var rucm = EnvVarReader.GetBool("REDIS_USE_COMMAND_MAP", false);
            if (rucm)
            {
                services.AddSingleton<ISyncLock, AtomicCustomLock>();
                logger.Info("ISyncLock will resolve to AtomicCustomLock");
            }
            else
            {
                services.AddSingleton<ISyncLock, NativeLock>();
                logger.Info("ISyncLock will resolve to NativeLock");
            }
            services.AddSingleton<IDatabaseConnectionFetcher, EnvVarDatabaseConnectionFetcher>();
            // NOTE: Replace DatabaseDummyClient by DatabaseClient to use a real database
            services.AddSingleton<IDatabaseClient, DatabaseDummyClient>();
            services.AddSingleton<IProcessedToSequencedListener, ProcessedListToSequencedListListener>();
            services.AddSingleton<ClientBase, ProcessedListToSequencedListListener>();
            services.AddSingleton<ISequencer, Sequencer>();
            var serviceProvider = services.BuildServiceProvider();
            return serviceProvider;
        }

        public async static Task<bool> Run(ServiceProvider serviceProvider)
        {
            var lsn = serviceProvider.GetService<ISequencer>() ?? throw new NullReferenceException("IListener implementation could not be resolved");
            await lsn.ReceiveMessageAsync();
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
            var ret = await Run(serviceProvider);

            logger.Info($"Application exiting with value {ret}");
        }
    }
}
