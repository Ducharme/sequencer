using System.Text;
using Microsoft.Extensions.Logging;

namespace CommonTypes
{
    public class LoggerTextWriter : TextWriter
    {
        private readonly ILogger logger;

        public LoggerTextWriter(ILogger logger)
        {
            this.logger = logger;
        }

        public override void WriteLine(string? value)
        {
            logger.LogInformation(value);
        }

        public override Encoding Encoding => Encoding.UTF8;
    }
}