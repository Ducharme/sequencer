using System.Diagnostics;

using CommonTypes;
using DatabaseAccessLayer;
using RedisAccessLayer;

using log4net;
using log4net.Config;
using Microsoft.Extensions.DependencyInjection;

namespace SequencerService
{
    public static class Program
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(Program));
        private static readonly List<ServiceProvider> _sps = [];

        public static void AssignEvents()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
        }

        public static void ConfigureLogging()
        {
            GlobalContext.Properties["pid"] = Process.GetCurrentProcess().Id;
            var dir = System.AppContext.BaseDirectory;
            var file = Path.Combine(dir, "log4net.config");
            XmlConfigurator.Configure(new FileInfo(file));
            logger.Info("Application started");
        }

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
            _sps.Add(serviceProvider);
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
            AssignEvents();
            ConfigureLogging();

            if (!EnvVarSetter.SetFromArgs(args, logger))
            {
                if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("REDIS_ENDPOINT")))
                    Environment.SetEnvironmentVariable("REDIS_ENDPOINT", "localhost");
                if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PGSQL_ENDPOINT")))
                    Environment.SetEnvironmentVariable("PGSQL_ENDPOINT", "localhost");
            }

            var serviceProvider = ConfigureServices(new ServiceCollection());
            var ret = await Run(serviceProvider);

            logger.Info($"Application exiting with value {ret}");
        }

        private static async void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine($"Unhandled exception: {e.ExceptionObject}");
            logger.Fatal($"Unhandled exception: {e.ExceptionObject}");
            await Shutdown();
            Environment.Exit(1);
        }

        private static async void CurrentDomain_ProcessExit(object? sender, System.EventArgs? e)
        {
            Console.WriteLine($"SIGTERM received. Shutting down gracefully");
            logger.Warn($"SIGTERM received. Shutting down gracefully");
            await Shutdown();
            Environment.Exit(1);
        }

        public async static Task Shutdown()
        {
            if (_sps.Count > 0)
            {
                var sp = _sps.First();
                var ppl = sp.GetService<ClientBase>();
                if (ppl != null)
                {
                    ppl.StopListening();
                    const int max = 30;
                    int counter = 0;
                    while (ppl.IsConnected && !ppl.IsExiting && counter < max)
                    {
                        await Task.Delay(100); // Wait for 100ms
                    }
                    ppl.Dispose();
                }
            }
        }
    }
}
