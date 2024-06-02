namespace RedisAccessLayer
{
    public class NativeLock : LockBase, ISyncLock
    {
        public NativeLock(IRedisConnectionManager cm)
            : base (cm)
        {
        }

        public async Task<bool> AcquireLock()
        {
            var acquired = await rcm.LockTakeAsync(lockKey, lockValue, lockExpiry);
            lockAcquisitionTime = acquired ? DateTime.UtcNow : DateTime.MinValue;
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
            return released;
        }

        public async override void Dispose()
        {
            await ReleaseLock();
            base.Dispose();
        }
    }
}