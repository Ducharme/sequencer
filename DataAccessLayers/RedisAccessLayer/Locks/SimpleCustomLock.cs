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
                return await rcm.KeyDeleteAsync(lockKey);
            }
            return false;
        }

        public async Task<bool> ExtendLock(TimeSpan newExpiry)
        {
            var currentLockValue = await GetLockValue();
            if (currentLockValue == lockValue)
            {
                bool lockExtended = await rcm.StringSetAsync(lockKey, lockValue, newExpiry, When.Exists);
                if (lockExtended)
                {
                    lockAcquisitionTime = DateTime.UtcNow;
                }
                return lockExtended;
            }
            return false;
        }

        public async Task<string?> GetLockValue()
        {
            return await rcm.StringGetAsync(lockKey);
        }

        public async void Dispose()
        {
            await ReleaseLock();
        }
    }
}