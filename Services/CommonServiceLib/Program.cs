using Microsoft.Extensions.DependencyInjection;
using log4net;
using log4net.Config;
using System.Diagnostics;

using RedisAccessLayer;

namespace CommonServiceLib
{
    public static class Program
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(Program));
        private static readonly List<IServiceProvider> _sps = [];

        public static void AssignEvents()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
        }

        public static void ConfigureLogging()
        {
            GlobalContext.Properties["pid"] = Process.GetCurrentProcess().Id;
            var dir = AppContext.BaseDirectory;
            var file = Path.Combine(dir, "log4net.config");
            XmlConfigurator.Configure(new FileInfo(file));
            logger.Info("Application started");
        }

        public static void LogNumberOfThreads()
        {
            var cpuCount = new CpuInfo().GetCpuCount().ToString("F2");
            var threadsCount = Process.GetCurrentProcess().Threads.Count;
            ThreadPool.GetMinThreads(out int minWorkerThreads, out int minCompletionPortThreads);
            ThreadPool.GetMaxThreads(out int maxWorkerThreads, out int maxCompletionPortThreads);
            logger.Info($"Process has {cpuCount} processors, {threadsCount} threads, {minWorkerThreads}/{maxWorkerThreads} Min/Max worker threads, {minCompletionPortThreads}/{maxCompletionPortThreads} Min/Max IO Threads");
        }

        public static void AddServiceProvider(IServiceProvider sp)
        {
            _sps.Add(sp);
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
            Console.WriteLine($"SIGTERM received, shutting down gracefully");
            logger.Info($"SIGTERM received, shutting down gracefully");
            await Shutdown();
            Environment.Exit(1);
        }

        public async static Task Shutdown()
        {
            if (_sps.Count > 0)
            {
                var sp = _sps.First();
                var cb = sp.GetService<ClientBase>();
                if (cb != null)
                {
                    cb.StopListening();
                    const int max = 30;
                    int counter = 0;
                    while (cb.IsConnected && !cb.IsExiting && counter < max)
                    {
                        await Task.Delay(100); // Wait for 100ms
                    }
                    cb.Dispose();
                }
            }
        }
    }
}
