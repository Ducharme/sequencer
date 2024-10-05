using log4net;

namespace RedisAccessLayer
{
    public class NativeLock : LockBase, ISyncLock
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(AtomicCustomLock));

        public NativeLock(IRedisConnectionManager cm)
            : base (cm)
        {
        }

        public async Task<bool> AcquireLock()
        {
            var acquired = await rcm.LockTakeAsync(lockKey, lockValue, lockExpiry);
            lockAcquisitionTime = acquired ? DateTime.UtcNow : DateTime.MinValue;
            if (logger.IsDebugEnabled)
            {
                logger.Debug($"AcquireLock for lockKey={lockKey} and lockValue={lockValue} (acquired={acquired} and lockAcquisitionTime={DisplayDateTime(lockAcquisitionTime)}");
            }
            return acquired;
        }

        public async Task<bool> ExtendLock(TimeSpan newExpiry)
        {
            var extended = await rcm.LockExtendAsync(lockKey, lockValue, newExpiry);
            if (extended)
            {
                lockExpiry = newExpiry;
                lockAcquisitionTime = DateTime.UtcNow;
            }
            if (logger.IsDebugEnabled)
            {
                logger.Debug($"ReleaseLock for lockKey={lockKey} and lockValue={lockValue} (extended={extended} with lockAcquisitionTime={DisplayDateTime(lockAcquisitionTime)}");
            }
            return extended;
        }

        public async Task<string?> GetLockValue()
        {
            return await rcm.LockQueryAsync(lockKey);
        }

        public async Task<bool?> ReleaseLock()
        {
            var released = await rcm.LockReleaseAsync(lockKey, lockValue);
            if (released)
            {
                lockAcquisitionTime = DateTime.MinValue;
            }
            if (logger.IsDebugEnabled)
            {
                logger.Debug($"ReleaseLock for lockKey={lockKey} and lockValue={lockValue} (released={released} with lockAcquisitionTime={DisplayDateTime(lockAcquisitionTime)}");
            }
            return released;
        }

        public async override void Dispose()
        {
            await ReleaseLock();
            base.Dispose();
        }
    }
}