namespace RedisAccessLayer
{
    public interface ISyncLock : IDisposable
    {
        bool IsLockAcquired { get; }
        Task<bool> AcquireLock();
        Task<bool> ExtendLock(TimeSpan newExpiry);
        Task<string?> GetLockValue();
        TimeSpan GetRemainingLockTime();
        TimeSpan GetDefaultLockTime();
        Task<bool?> ReleaseLock();
    }
}