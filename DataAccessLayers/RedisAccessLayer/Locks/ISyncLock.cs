namespace RedisAccessLayer
{
    public interface ISyncLock
    {
        Task<bool> AcquireLock();
        Task<bool> ExtendLock(TimeSpan newExpiry);
        Task<bool?> ReleaseLock();
        /// <summary>Fetching value from server<summary>
        Task<string?> GetLockValue();
         /// <summary>Shared readonly value<summary>
        public string LockKey { get; }
        /// <summary>Client's readonly value<summary>
        public string LockValue { get; }
        /// <summary>Client's readonly value<summary>
        public TimeSpan LockExpiry { get; }
        /// <summary>Calculated value locally<summary>
        public TimeSpan RemainingLockTime { get; }
        /// <summary>Calculated value locally<summary>
        bool IsLockAcquired { get; }
    }
}