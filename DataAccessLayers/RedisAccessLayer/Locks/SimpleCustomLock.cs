using log4net;
using StackExchange.Redis;

namespace RedisAccessLayer
{
    public class SimpleCustomLock : LockBase, ISyncLock
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(SimpleCustomLock));
        
        public SimpleCustomLock(IRedisConnectionManager cm)
            : base (cm)
        {
        }

        public async Task<bool> AcquireLock()
        {
            // Try to set the lock key with the generated value and an expiration
            bool acquired = await rcm.StringSetAsync(lockKey, lockValue, lockExpiry, When.NotExists);
            lockAcquisitionTime = acquired ? DateTime.UtcNow : DateTime.MinValue;
            if (logger.IsDebugEnabled)
            {
                logger.Debug($"AcquireLock for lockKey={lockKey} and lockValue={lockValue} (acquired={acquired} and lockAcquisitionTime={lockAcquisitionTime}");
            }
            return acquired;
        }

        public async Task<bool?> ReleaseLock()
        {
            // Check if the lock is still held by the current instance
            var currentLockValue = await GetLockValue();
            logger.Debug($"Current lock value: {currentLockValue} and lockValue is {lockValue}");
            if (currentLockValue == lockValue)
            {
                // Release the lock by deleting the key
                var released = await rcm.KeyDeleteAsync(lockKey);
                if (released)
                {
                    lockAcquisitionTime = DateTime.MinValue;
                }
                if (logger.IsDebugEnabled)
                {
                    logger.Debug($"ReleaseLock for lockKey={lockKey} and lockValue={lockValue} (released={released} with lockAcquisitionTime={lockAcquisitionTime}");
                }
                return released;
            }
            else
            {
                if (logger.IsDebugEnabled)
                {
                    logger.Debug($"ReleaseLock for lockKey={lockKey} and lockValue={lockValue} (currently not acquired, lockAcquisitionTime={lockAcquisitionTime}");
                }
            }

            return false;
        }

        public async Task<bool> ExtendLock(TimeSpan newExpiry)
        {
            var currentLockValue = await GetLockValue();
            if (currentLockValue == lockValue)
            {
                bool extended = await rcm.StringSetAsync(lockKey, lockValue, newExpiry, When.Exists);
                if (extended)
                {
                    lockAcquisitionTime = DateTime.UtcNow;
                }
                if (logger.IsDebugEnabled)
                {
                    logger.Debug($"ExtendLock for lockKey={lockKey} and lockValue={lockValue} (extended={extended} with lockAcquisitionTime={lockAcquisitionTime}");
                }
                return extended;
            }
            else
            {
                if (logger.IsDebugEnabled)
                {
                    logger.Debug($"ExtendLock for lockKey={lockKey} and lockValue={lockValue} (currently not acquired, lockAcquisitionTime={lockAcquisitionTime}");
                }
            }
            return false;
        }

        public async Task<string?> GetLockValue()
        {
            return await rcm.StringGetAsync(lockKey);
        }

        public async override void Dispose()
        {
            await ReleaseLock();
            base.Dispose();
        }
    }
}