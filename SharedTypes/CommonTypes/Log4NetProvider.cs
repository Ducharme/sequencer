using Microsoft.Extensions.Logging;
using log4net;

namespace CommonTypes
{
    public class Log4NetProvider : ILoggerProvider
    {
        private readonly ILog logger;

        public Log4NetProvider(ILog log)
        {
            logger = log;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new Log4NetLogger(logger, categoryName);
        }

        public void Dispose()
        {
        }

        private class Log4NetLogger : ILogger
        {
            private readonly ILog _log;
            private readonly string _categoryName;

            public Log4NetLogger(ILog log, string categoryName)
            {
                _log = log;
                _categoryName = categoryName;
            }

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception, string> formatter)
            {
                var message = formatter(state, exception ?? new Exception("Empty"));
                switch (logLevel)
                {
                    case LogLevel.Trace:
                        _log.Debug($"[{_categoryName}] {message}", exception);
                        break;
                    case LogLevel.Debug:
                        _log.Debug($"[{_categoryName}] {message}", exception);
                        break;
                    case LogLevel.Information:
                        _log.Info($"[{_categoryName}] {message}", exception);
                        break;
                    case LogLevel.Warning:
                        _log.Warn($"[{_categoryName}] {message}", exception);
                        break;
                    case LogLevel.Error:
                        _log.Error($"[{_categoryName}] {message}", exception);
                        break;
                    case LogLevel.Critical:
                        _log.Fatal($"[{_categoryName}] {message}", exception);
                        break;
                    default:
                        _log.Info($"[{_categoryName}] {message}", exception);
                        break;
                }
            }
        }
    }
}