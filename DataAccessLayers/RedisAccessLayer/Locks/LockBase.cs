using CommonTypes;

namespace RedisAccessLayer
{
    public abstract class LockBase : ClientBase
    {
        protected readonly string lockKey;
        protected readonly string lockValue;
        protected TimeSpan lockExpiry;
        protected DateTime lockAcquisitionTime;
        public static readonly TimeSpan DefaultLockExpiry = TimeSpan.FromSeconds(1);

        public LockBase(IRedisConnectionManager cm)
            : base (cm)
        {
            var channelPrefix = EnvVarReader.GetString("REDIS_CHANNEL_PREFIX", EnvVarReader.NotFound);
            var prefix = channelPrefix != EnvVarReader.NotFound ? string.Concat("{", channelPrefix, "}:") : string.Empty;
            lockKey = prefix + "sequencer-leader";
            lockValue = this.ClientName + "_" + Guid.NewGuid().ToString();
            lockExpiry = DefaultLockExpiry;
        }

        public string LockKey { get { return lockKey; } }
        public string LockValue { get { return lockValue; } }
        public TimeSpan LockExpiry { get { return lockExpiry; } }
        public TimeSpan RemainingLockTime { get { return lockAcquisitionTime.Add(lockExpiry) - DateTime.UtcNow; } }
        public bool IsLockAcquired => lockAcquisitionTime != DateTime.MinValue && RemainingLockTime > TimeSpan.Zero;
    }
}