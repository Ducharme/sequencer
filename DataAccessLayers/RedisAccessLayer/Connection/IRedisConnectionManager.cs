using CommonTypes;
using StackExchange.Redis;

namespace RedisAccessLayer
{
    public interface IRedisConnectionManager : IDisposable
    {
        IConnectionMultiplexer Connection { get; }
        IDatabase Database { get; }
        ISubscriber Subscriber { get; }
        string ClientName { get; }
        int GetHealthStatus();
        Task<bool> Reconnect();
        void AddSubscription(RedisChannel channel, Action<RedisChannel, RedisValue> handler);
        Task SubscribeAsync();
        
        Task<StreamEntry[]> StreamReadAsync(RedisKey key, RedisValue position);
        Task<List<StreamEntry>> StreamReadAllAsync(RedisKey key, int batchSize = 1000);
        Task<RedisValue[]> ListRangeAsync(RedisKey key, long start = 0, long stop = -1);
        Task<List<RedisValue>> ListAllAsync(RedisKey key, int batchSize = 1000);
        Task<List<RedisValue>> ListAllPipelinedAsync(RedisKey key, int batchSize = 1000);
        Task<string> ListGetByIndexAsync(RedisKey key, long index);
        Task<bool> KeyDeleteAsync(RedisKey key);
        Task<string> ListRightPopLeftPushListSetByIndexInTransactionAsync(RedisKey source, RedisKey destination, long val);
        Task<string[]> ListRightPopLeftPushListSetByIndexInTransactionBatchAsync(RedisKey source, RedisKey destination, long val, int batchSize);
        Task<bool> ListLeftPushPublishInTransactionAsync(RedisKey key, RedisValue val, RedisChannel publishChannel, RedisValue publishValue);
        Task<bool> ListLeftPushPublishInTransactionAsync(RedisKey key, IEnumerable<MyMessage> mms, RedisChannel publishChannel, RedisValue publishValue);
        Task<long> ListRemoveAsync(RedisKey key, RedisValue val);
        Task<string> ListRightPopAsync(RedisKey key);
        Task<bool> KeyExistsAsync(RedisKey key);
        Task<StreamInfo> StreamInfoAsync(RedisKey key);
        Task<RedisValue> StringGetAsync(RedisKey key);
        Task<bool> StringSetAsync(RedisKey key, RedisValue val, TimeSpan? expiry, When when);
        Task<RedisResult> ScriptEvaluateAsync(string scriptName, string script, RedisKey[]? keys, RedisValue[]? values);
        Task<bool> LockTakeAsync(RedisKey key, RedisValue val, TimeSpan expiry);
        Task<bool> LockExtendAsync(RedisKey key, RedisValue val, TimeSpan expiry);
        Task<RedisValue> LockQueryAsync(RedisKey key);
        Task<bool> LockReleaseAsync(RedisKey key, RedisValue val);
        Task<bool> StreamAddListRemovePublishInTransactionAsync(RedisKey streamAddKey, RedisValue streamAddVal, string listRemoveKey, RedisValue listRemoveVal, RedisChannel publishChannel, RedisValue publishValue);
        Task<bool> StreamAddListLeftPushStreamDeletePublishInTransactionAsync(RedisKey streamAddKey, RedisKey listLeftPushKey, List<Tuple<string, NameValueEntry[]>> values, RedisKey streamDeleteKey, RedisValue[] streamDeleteValues, RedisChannel publishChannel, RedisValue publishValue);

         Task<long> GetListMessagesCount(RedisKey key);
        Task<long> GetStreamMessagesCount(RedisKey key);
        Task<MyMessage?> GetStreamLastMessage(RedisKey key);

        Task<string> ServerInfos();
        Task<TimeSpan> Ping();
    }
}