using log4net;

namespace CommonTypes
{
    public static class EnvVarReader
    {
        public const string NotFound = "NotFound";

        private static readonly ILog logger = LogManager.GetLogger(typeof(EnvVarReader));
        
        public static string GetString(string name, string defaultValue)
        {
            var value = Environment.GetEnvironmentVariable(name);
            return !string.IsNullOrEmpty(value) ? value.Trim() : defaultValue;
        }

        public static int GetInt(string name, int defaultValue)
        {
            var value = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrEmpty(value))
            {
                return defaultValue;
            }
            else
            {
                if (int.TryParse(value, out int parsedValue))
                {
                    return parsedValue;
                }
                else
                {
                    logger.Error($"Invalid value for {name}: {value}, using default value {defaultValue}");
                    return defaultValue;
                }
            }
        }

        public static bool GetBool(string name, bool defaultValue)
        {
            var value = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrEmpty(value))
            {
                return defaultValue;
            }
            else
            {
                if (bool.TryParse(value.ToLower(), out bool parsedValue))
                {
                    return parsedValue;
                }
                else
                {
                    logger.Error($"Invalid value for {name}: {value}, using default value {defaultValue}");
                    return defaultValue;
                }
            }
        }
    }
}