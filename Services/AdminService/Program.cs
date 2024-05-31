using RedisAccessLayer;
using DatabaseAccessLayer;

using Microsoft.Extensions.DependencyInjection;
using log4net;
using log4net.Config;
using CommonTypes;


namespace AdminService
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
            services.AddSingleton<IConnectionMultiplexerWrapper, ConnectionMultiplexerWrapper>();
            services.AddSingleton<IRedisConfigurationFetcher, EnvVarRedisConfigurationFetcher>();
            services.AddSingleton<IRedisConnectionManager, RedisConnectionManager>();
            services.AddSingleton<IDatabaseConnectionFetcher, EnvVarDatabaseConnectionFetcher>();
            // NOTE: Replace DatabaseDummyAdmin by DatabaseAdmin to use a real database
            services.AddSingleton<IDatabaseAdmin, DatabaseDummyAdmin>();
            services.AddSingleton<IListStreamAdminClient, ListStreamAdminClient>();
            services.AddSingleton<IAdminManager, AdminManager>();
            var serviceProvider = services.BuildServiceProvider();
            return serviceProvider;
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

            var groupName = Environment.GetEnvironmentVariable("GROUP_NAME") ?? string.Empty;
            var adm = serviceProvider.GetService<IAdminManager>() ?? throw new NullReferenceException("IAdminManager implementation could not be resolved");
            await adm.PrepareDatabase(groupName);
            await adm.DeleteStreams(groupName);
            await adm.DeleteLists(groupName);
            await adm.GeneratePendingList(groupName, 10, 0, 500);

            var cm = serviceProvider.GetService<IRedisConnectionManager>() ?? throw new NullReferenceException("IRedisConnectionManager implementation could not be resolved");

            //await TestLocks(cm);
            var mms = await adm.GetAllMessagesFromProcessedStream(groupName);
            foreach (var mm in mms)
            {
                logger.Debug(mm.ToShortString());
            }

            logger.Info("Application exiting");
        }

        private static async Task TestLocks(IRedisConnectionManager rcm)
        {
            var cl1 = new SimpleCustomLock(rcm);
            var cl2 = new SimpleCustomLock(rcm);

            var val1 = await cl1.GetLockValue();
            Console.WriteLine($"Lock value 1: {val1}");
            var val2 = await cl2.GetLockValue();
            Console.WriteLine($"Lock value 2: {val2}");

            var aq1 = await cl1.AcquireLock();
            Console.WriteLine($"Acquire value 1: {aq1}");
            var aq2 = await cl2.AcquireLock();
            Console.WriteLine($"Acquire value 2: {aq2}");

            await Task.Delay(100);
            for (int i = 0; i < 12; i++)
            {
                Console.WriteLine($"#{i} Remaining time 1: {cl1.GetRemainingLockTime()}");
                aq2 = await cl2.AcquireLock();
                Console.WriteLine($"#{i} Acquire value 2: {aq2}");
                await Task.Delay(100);
            }
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine($"Unhandled exception: {e.ExceptionObject}");
            logger.Fatal($"Unhandled exception: {e.ExceptionObject}");

            Environment.Exit(1);
        }
    }
}
