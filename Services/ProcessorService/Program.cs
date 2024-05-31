using CommonTypes;
using DatabaseAccessLayer;
using RedisAccessLayer;

using log4net;
using log4net.Config;
using Microsoft.Extensions.DependencyInjection;


namespace ProcessorService
{
    public static class Program
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(Program));
        
        public static void ConfigureLogging()
        {
            var dir = System.AppContext.BaseDirectory;
            var file = Path.Combine(dir, "log4net.config");
            XmlConfigurator.Configure(new FileInfo(file));
            logger.Info("Application started");
        }
        
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
            services.AddSingleton<IProcessor, Processor>();
            var serviceProvider = services.BuildServiceProvider();
            return serviceProvider;
        }

        public async static Task<bool> Run(ServiceProvider serviceProvider)
        {
            var rcv = serviceProvider.GetService<IProcessor>() ?? throw new NullReferenceException("IProcessor implementation could not be resolved");
            await rcv.ReceiveMessageAsync();
            return true;
        }

        public static async Task Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            ConfigureLogging();

            if (!EnvVarSetter.SetFromArgs(args, logger))
            {
                if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("REDIS_ENDPOINT")))
                    Environment.SetEnvironmentVariable("REDIS_ENDPOINT", "localhost");
                if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PGSQL_ENDPOINT")))
                    Environment.SetEnvironmentVariable("PGSQL_ENDPOINT", "localhost");
            }

            var serviceProvider = ConfigureServices(new ServiceCollection());
            object ret = await Run(serviceProvider);

            logger.Info($"Application exiting with value {ret}");
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine($"Unhandled exception: {e.ExceptionObject}");
            logger.Fatal($"Unhandled exception: {e.ExceptionObject}");

            Environment.Exit(1);
        }
    }
}
