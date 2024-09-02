using log4net;
using StackExchange.Redis;

namespace RedisAccessLayer
{
    public class AtomicCustomLock : LockBase, ISyncLock
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(AtomicCustomLock));

        // Use Lua scripts to ensure atomicity
        internal const string AcquireScript = @"
        if redis.call('exists', KEYS[1]) == 0 then
            redis.call('set', KEYS[1], ARGV[1], 'PX', ARGV[2])
            return ARGV[3]
        elseif redis.call('get', KEYS[1]) == ARGV[1] then
            redis.call('pexpire', KEYS[1], ARGV[2])
            return ARGV[3]
        else
            return nil
        end";

        internal const string ExtendScript = @"
        if redis.call('get', KEYS[1]) == ARGV[1] then
            redis.call('pexpire', KEYS[1], ARGV[2])
            return ARGV[3]
        else
            return nil
        end";

        internal const string ReleaseScript = @"
        if redis.call('get', KEYS[1]) == ARGV[1] then
            redis.call('del', KEYS[1])
            return ARGV[2]
        else
            return nil
        end";
        
        public AtomicCustomLock(IRedisConnectionManager cm)
            : base (cm)
        {
        }

        public async Task<bool> AcquireLock()
        {
            string requestId = Guid.NewGuid().ToString();
            var rk = new RedisKey[] { lockKey };
            var rv = new RedisValue[] { lockValue, (int)lockExpiry.TotalMilliseconds, requestId };
            var result = await rcm.ScriptEvaluateAsync("AcquireScript", AcquireScript, rk, rv);
            var acquired = !result.IsNull && result.ToString() == requestId;
            lockAcquisitionTime = acquired ? DateTime.UtcNow : DateTime.MinValue;
            if (logger.IsDebugEnabled)
            {
                logger.Debug($"AcquireLock result={result} for lockKey={lockKey} and lockValue={lockValue} (acquired={acquired} and lockAcquisitionTime={lockAcquisitionTime}");
            }
            return acquired;
        }

        public async Task<bool?> ReleaseLock()
        {
            string requestId = Guid.NewGuid().ToString();
            var rk = new RedisKey[] { lockKey };
            var rv = new RedisValue[] { lockValue, requestId };
            var result = await rcm.ScriptEvaluateAsync("ReleaseScript", ReleaseScript, rk, rv);
            var released = !result.IsNull && result.ToString() == requestId;
            if (released)
            {
                lockAcquisitionTime = DateTime.MinValue;
            }
            if (logger.IsDebugEnabled)
            {
                logger.Debug($"ReleaseLock result={result} for lockKey={lockKey} and lockValue={lockValue} (released={released} with lockAcquisitionTime={lockAcquisitionTime}");
            }
            return released;
        }

        public async Task<bool> ExtendLock(TimeSpan newExpiry)
        {
            string requestId = Guid.NewGuid().ToString();
            var rk =  new RedisKey[] { lockKey };
            var rv = new RedisValue[] { lockValue, (int)newExpiry.TotalMilliseconds, requestId };
            var result = await rcm.ScriptEvaluateAsync("ExtendScript", ExtendScript, rk, rv);
            bool extended = !result.IsNull && result.ToString() == requestId;
            if (extended)
            {
                lockAcquisitionTime = DateTime.UtcNow;
            }
            if (logger.IsDebugEnabled)
            {
                logger.Debug($"ExtendLock result={result} for lockKey={lockKey} and lockValue={lockValue} (extended={extended} with lockAcquisitionTime={lockAcquisitionTime}");
            }
            return extended;
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