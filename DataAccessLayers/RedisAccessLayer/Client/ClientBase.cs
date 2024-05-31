using CommonTypes;

using log4net;

namespace RedisAccessLayer
{
    public abstract class ClientBase
    {
        protected readonly string groupName = string.Empty;
        protected readonly IRedisConnectionManager rcm;
        protected string ClientName => rcm.Connection.ClientName;


        public long? LastProcessedSequenceId { get; protected set; }

        private static readonly ILog logger = LogManager.GetLogger(typeof(ClientBase));

        public ClientBase(IRedisConnectionManager cm)
        {
            this.rcm = cm;
            this.groupName = Environment.GetEnvironmentVariable("GROUP_NAME") ?? string.Empty;
        }

        protected string AppendName(string key)
        {
            var name = string.IsNullOrEmpty(this.groupName) ? key : string.Concat(key, "-", this.groupName);
            var channelPrefix = EnvVarReader.GetString("REDIS_CHANNEL_PREFIX", EnvVarReader.NotFound);
            var prefix = channelPrefix != EnvVarReader.NotFound ? string.Concat("{", channelPrefix, "}:") : string.Empty;
            return prefix + name;
        }
    }
}