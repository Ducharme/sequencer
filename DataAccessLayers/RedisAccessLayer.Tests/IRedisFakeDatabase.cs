using StackExchange.Redis;

namespace RedisAccessLayer.Tests
{
    public interface IRedisFakeDatabase
    {
        Task SubscribeAsync(RedisChannel channel, Action<RedisChannel, RedisValue> handler, CommandFlags flags = CommandFlags.None);
        Task<TimeSpan> PingAsync(CommandFlags flags = CommandFlags.None);
        Task<StreamEntry[]> StreamReadAsync(RedisKey key, RedisValue position, int? count = null, CommandFlags flags = CommandFlags.None);
        Task<RedisValue> ListGetByIndexAsync(RedisKey key, long index, CommandFlags flags = CommandFlags.None);
        Task<RedisValue[]> ListRangeAsync(RedisKey key, long start = 0, long stop = -1, CommandFlags flags = CommandFlags.None);
        Task<bool> KeyDeleteAsync(RedisKey key, CommandFlags flags = CommandFlags.None);
        Task<RedisResult> ScriptEvaluateAsync(string script, RedisKey[]? keys = null, RedisValue[]? values = null, CommandFlags flags = CommandFlags.None);
        ITransaction CreateTransaction(object? asyncState = null);
        Task<long> ListRemoveAsync(RedisKey key, RedisValue value, long count = 0, CommandFlags flags = CommandFlags.None);
        Task<RedisValue> ListRightPopAsync(RedisKey key, CommandFlags flags = CommandFlags.None);
        Task<bool> KeyExistsAsync(RedisKey key, CommandFlags flags = CommandFlags.None);
        Task<StreamInfo> StreamInfoAsync(RedisKey key, CommandFlags flags = CommandFlags.None);
        Task<bool> StringSetAsync(RedisKey key, RedisValue value, TimeSpan? expiry, When when, CommandFlags flags);
        Task<RedisValue> StringGetAsync(RedisKey key, CommandFlags flags = CommandFlags.None);
        Task<bool> LockTakeAsync(RedisKey key, RedisValue value, TimeSpan expiry, CommandFlags flags = CommandFlags.None);
        Task<bool> LockExtendAsync(RedisKey key, RedisValue value, TimeSpan expiry, CommandFlags flags = CommandFlags.None);
        Task<RedisValue> LockQueryAsync(RedisKey key, CommandFlags flags = CommandFlags.None);
        Task<bool> LockReleaseAsync(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None);
    }
}