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
            lockKey = prefix + "sequencer-master";
            lockValue = this.ClientName + "_" + Guid.NewGuid().ToString();
            lockExpiry = DefaultLockExpiry;
        }

        public TimeSpan GetRemainingLockTime()
        {
            return lockAcquisitionTime.Add(lockExpiry) - DateTime.UtcNow;
        }

        public TimeSpan GetDefaultLockTime()
        {
            return DefaultLockExpiry;
        }

        public bool IsLockAcquired
        {
            get
            {
                if (lockAcquisitionTime == DateTime.MinValue)
                {
                    return false;
                }

                var remainingTime = GetRemainingLockTime();
                return remainingTime > TimeSpan.Zero;
            }
        }
    }
}