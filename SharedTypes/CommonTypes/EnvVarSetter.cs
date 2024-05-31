using log4net;

namespace CommonTypes
{
    public static class EnvVarSetter
    {
        public static bool SetFromArgs(string[] args, ILog logger)
        {
            var wasSet = false;
            if (args != null && args.Length > 0)
            {
                foreach(var arg in args)
                {
                    if (arg.StartsWith(".env."))
                    {
                        logger.Info($"Env File as argument is {arg}");
                        var dir = System.AppContext.BaseDirectory;
                        var file = Path.Combine(dir, arg);
                        var content = File.ReadAllText(file);
                        var separators = new char[] {'\r', '\n'};
                        var lines = content.Split(separators, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var line in lines)
                        {
                            var tokens = line.Split('=', StringSplitOptions.TrimEntries);
                            if (tokens.Length == 2)
                            {
                                logger.Info($"Setting {tokens[0]}={tokens[1]}");
                                Environment.SetEnvironmentVariable(tokens[0], tokens[1]);
                                wasSet = true;
                            }
                        }
                    }
                }
            }
            return wasSet;
        }
    }
}