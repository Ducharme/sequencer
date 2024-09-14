using CommonTypes;

using log4net;

namespace RedisAccessLayer
{
    public abstract class ClientBase: IDisposable
    {
        protected readonly string groupName = string.Empty;
        protected readonly IRedisConnectionManager rcm;
        protected string ClientName => rcm.ClientName;
        protected long shouldListen = 1;
        protected long isExiting = 0;


        public long? LastProcessedSequenceId { get; protected set; }
        public bool IsExiting { get { return Interlocked.Read(ref this.isExiting) == 1; } }
        public bool IsConnected { get { var status = rcm.GetHealthStatus(); return status >= 200; } }

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

        public void StopListening()
        {
            Interlocked.Exchange(ref this.shouldListen, 0);
        }

        public virtual void Dispose()
        {
            rcm.Dispose();
        }
    }
}