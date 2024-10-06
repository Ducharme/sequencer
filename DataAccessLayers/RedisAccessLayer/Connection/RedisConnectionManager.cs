using CommonTypes;
using log4net;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace RedisAccessLayer
{
    public class RedisConnectionManager : IRedisConnectionManager, IDisposable
    {
        public IConnectionMultiplexer Connection { get; private set; }
        public IDatabase Database { get; private set; }
        public ISubscriber Subscriber { get; private set; }
        public string ClientName { get; }

        public readonly IRedisConfigurationFetcher ConfigurationFetcher;
        public readonly IConnectionMultiplexerWrapper ConnectionMultiplexerWrapper;
        private readonly Dictionary<RedisChannel, Action<RedisChannel, RedisValue>> subscriptions = [];
        protected const CommandFlags DefaultCommandFlags = CommandFlags.DemandMaster;

        protected volatile bool isDisposed = false;
        protected volatile bool isConnected = false;
        protected volatile bool hasError = false;

        private static readonly ILog logger = LogManager.GetLogger(typeof(RedisConnectionManager));
        private static readonly ILog loggerRedis = LogManager.GetLogger("StackExchange.Redis");

        private const string ScriptPendingToProcessing = @"
local source = KEYS[1]
local destination = KEYS[2]
local replacementTime = ARGV[1]

local value = redis.call('RPOP', source)

if value then
    local updatedValue = string.gsub(value, ';0;0;0;0;0', ';' .. replacementTime .. ';0;0;0;0')
    redis.call('LPUSH', destination, updatedValue)
    return updatedValue
else
    return nil
end";

    private static readonly string ScriptBatchPendingToProcessing = @"
local source = KEYS[1]
local destination = KEYS[2]
local replacementTime = ARGV[1]
local batchSize = ARGV[2]

local results = {}
local count = 0

for i = 1, batchSize do
    local value = redis.call('RPOP', source)
    if value then
        local updatedValue = string.gsub(value, ';0;0;0;0;0', ';' .. replacementTime .. ';0;0;0;0')
        redis.call('LPUSH', destination, updatedValue)
        table.insert(results, updatedValue)
        count = count + 1
    else
        break
    end
end

if count > 0 then
    return results
else
    return nil
end";

        public RedisConnectionManager(IRedisConfigurationFetcher cf, IConnectionMultiplexerWrapper cm)
        {
            ConfigurationFetcher = cf;
            ConnectionMultiplexerWrapper = cm;

            logger.Info($"Creating connection");
            Connection = cm.Connect(cf.OptionsWrapper, loggerRedis);
            Database = Connection.GetDatabase();
            Subscriber = Connection.GetSubscriber();
            ClientName = Connection.ClientName;

            isConnected = Connection.IsConnected;

            AssignEvents();
        }

        public int GetHealthStatus()
        {
            if (!isConnected)
            {
                return 503; // Service Unavailable
            }
            else if (hasError)
            {
                return 500; // Internal Server Error
            }
            return 200; // OK
        }

        public async Task<bool> Reconnect()
        {
            try
            {
                if (Connection != null)
                {
                    logger.Debug($"Closing connection");
                    await Connection.CloseAsync();
                    logger.Debug($"Disposing connection");
                    await Connection.DisposeAsync();
                }

                logger.Info($"Recreating connection");
                Connection = ConnectionMultiplexerWrapper.Connect(ConfigurationFetcher.OptionsWrapper, loggerRedis);
                Database = Connection.GetDatabase();
                Subscriber = Connection.GetSubscriber();

                logger.Info($"Pinging connection");
                var response = await Database.PingAsync(DefaultCommandFlags);
                isConnected = Connection.IsConnected;
                logger.Info($"IsConnected={isConnected} with ping latency of {response}");

                await SubscribeAsync();
                return isConnected;
            }
            catch (Exception ex)
            {
                logger.Error($"Redis reconnection failed", ex);
                isConnected = false;
                return false;
            }
            finally
            {
                AssignEvents();
            }
        }

        private void AssignEvents()
        {
            Connection.ConnectionFailed += (sender, connectionFailedEventArgs) =>
            {
                isConnected = false;
                logger.Error($"Redis Connection Failed: {connectionFailedEventArgs.Exception}");
            };

            Connection.ConnectionRestored += (sender, connectionFailedEventArgs) =>
            {
                isConnected = true;
                logger.Info("Redis Connection Restored");
            };

            Connection.ErrorMessage += (sender, errorEventArgs) =>
            {
                hasError = true;
                logger.Error($"Redis Error: {errorEventArgs.Message}");
            };

            Connection.InternalError += (sender, internalErrorEventArgs) =>
            {
                hasError = true;
                logger.Error($"Redis Internal Error: {internalErrorEventArgs.Exception}");
            };
        }

        public void AddSubscription(RedisChannel channel, Action<RedisChannel, RedisValue> handler)
        {
            if (logger.IsDebugEnabled)
            {
                logger.Debug($"Adding subscription to channel {channel} with handler {handler.Method.Name} on {handler.Target}");
            }
            subscriptions[channel] = handler;
        }

        public async Task SubscribeAsync()
        {
            RedisChannel? rc = null;
            try
            {
                if (subscriptions.Count > 0)
                {
                    logger.Info($"Subscribing to {subscriptions.Count} channels");
                    foreach (var subscription in subscriptions)
                    {
                        rc = subscription.Key;
                        await Subscriber.SubscribeAsync(subscription.Key, subscription.Value, DefaultCommandFlags);
                    }
                }
                hasError = false;
            }
            catch (Exception ex) when (ex is RedisServerException || ex is RedisTimeoutException || ex is RedisCommandException || ex is RedisConnectionException || ex is RedisException)
            {
                hasError = true;
                logger.Error($"SubscribeAsync to channel {rc} failed", ex);
                throw;
            }
        }

        public async Task<StreamEntry[]> StreamReadAsync(RedisKey key, RedisValue position)
        {
            try
            {
                if (logger.IsDebugEnabled)
                {
                    logger.Debug($"StreamReadAsync {key} with position={position}");
                }
                var response = await Database.StreamReadAsync(key, position, count: null, DefaultCommandFlags) ?? [];
                hasError = false;
                return response;
            }
            catch (Exception ex) when (ex is RedisServerException || ex is RedisTimeoutException || ex is RedisCommandException || ex is RedisConnectionException || ex is RedisException)
            {
                hasError = true;
                logger.Error($"StreamReadAsync {key} with position={position} failed", ex);
                throw;
            }
        }

        public async Task<string> ListGetByIndexAsync(RedisKey key, long index)
        {
            try
            {
                if (logger.IsDebugEnabled)
                {
                    logger.Debug($"ListGetByIndexAsync with key={key}");
                }
                var response = await Database.ListGetByIndexAsync(key, index, DefaultCommandFlags);
                hasError = false;
                return response.IsNullOrEmpty ? string.Empty : response.ToString() ?? string.Empty;
            }
            catch (Exception ex) when (ex is RedisServerException || ex is RedisTimeoutException || ex is RedisCommandException || ex is RedisConnectionException || ex is RedisException)
            {
                hasError = true;
                logger.Error($"ListGetByIndexAsync with key={key} failed", ex);
                throw;
            }
        }

        public async Task<RedisValue[]> ListRangeAsync(RedisKey key, long start = 0, long stop = -1)
        {
            try
            {
                if (logger.IsDebugEnabled)
                {
                    logger.Debug($"ListRangeAsync key={key} with start={start} and stop={stop}");
                }
                var response = await Database.ListRangeAsync(key, start, stop, DefaultCommandFlags) ?? [];
                hasError = false;
                return response;
            }
            catch (Exception ex) when (ex is RedisServerException || ex is RedisTimeoutException || ex is RedisCommandException || ex is RedisConnectionException || ex is RedisException)
            {
                hasError = true;
                logger.Error($"ListRangeAsync key={key} with start={start} and stop={stop} failed", ex);
                throw;
            }
        }

        public async Task<bool> KeyDeleteAsync(RedisKey key)
        {
            try
            {
                if (logger.IsDebugEnabled)
                {
                    logger.Debug($"KeyDeleteAsync key={key}");
                }
                var response = await Database.KeyDeleteAsync(key, DefaultCommandFlags);
                hasError = false;
                return response;
            }
            catch (Exception ex) when (ex is RedisServerException || ex is RedisTimeoutException || ex is RedisCommandException || ex is RedisConnectionException || ex is RedisException)
            {
                hasError = true;
                logger.Error($"KeyDeleteAsync key={key} failed", ex);
                throw;
            }
        }

        public async Task<string> ListRightPopLeftPushListSetByIndexInTransactionAsync(RedisKey source, RedisKey destination, long val)
        {
            try
            {
                if (logger.IsDebugEnabled)
                {
                    logger.Debug($"ListRightPopLeftPushListSetByIndexInTransactionAsync source={source} destination={destination} val={val}");
                }
                RedisResult result = await Database.ScriptEvaluateAsync(ScriptPendingToProcessing, [source, destination], [val], DefaultCommandFlags);
                var response = result.IsNull ? string.Empty : result.ToString() ?? string.Empty;
                hasError = false;
                return response;
            }
            catch (Exception ex) when (ex is RedisServerException || ex is RedisTimeoutException || ex is RedisCommandException || ex is RedisConnectionException || ex is RedisException)
            {
                hasError = true;
                logger.Error($"ListRightPopLeftPushListSetByIndexInTransactionAsync source={source} destination={destination} val={val} failed", ex);
                throw;
            }
        }

        public async Task<string[]> ListRightPopLeftPushListSetByIndexInTransactionBatchAsync(RedisKey source, RedisKey destination, long val, int batchSize)
        {
            try
            {
                if (logger.IsDebugEnabled)
                {
                    logger.Debug($"ListRightPopLeftPushListSetByIndexInTransactionBatchAsync source={source} destination={destination} val={val}");
                }
                RedisResult result = await Database.ScriptEvaluateAsync(ScriptBatchPendingToProcessing, [source, destination], [val, batchSize], DefaultCommandFlags);
                hasError = false;
                string[] response;
                if (result.IsNull)
                {
                    response = [];
                }
                else
                {
                    var tmp = (string[]?)result;
                    if (tmp == null)
                    {
                        response = [];
                    }
                    else
                    {
                        response = tmp;
                    }
                }
                return response;
            }
            catch (Exception ex) when (ex is RedisServerException || ex is RedisTimeoutException || ex is RedisCommandException || ex is RedisConnectionException || ex is RedisException)
            {
                hasError = true;
                logger.Error($"ListRightPopLeftPushListSetByIndexInTransactionBatchAsync source={source} destination={destination} val={val} failed", ex);
                throw;
            }
        }

        public async Task<bool> ListLeftPushPublishInTransactionAsync(RedisKey key, RedisValue val, RedisChannel publishChannel, RedisValue publishValue)
        {
            try
            {
                if (logger.IsDebugEnabled)
                {
                    logger.Debug($"ListLeftPushPublishInTransactionAsync key={key} val={val} publishChannel={publishChannel} publishValue={publishValue}");
                }
                var transaction = Database.CreateTransaction();
                #pragma warning disable CS4014
                transaction.ListLeftPushAsync(key, val, When.Always, DefaultCommandFlags);
                transaction.PublishAsync(publishChannel, publishValue, DefaultCommandFlags);
                #pragma warning restore CS4014

                var response = await transaction.ExecuteAsync(DefaultCommandFlags);
                hasError = false;
                return response;
            }
            catch (Exception ex) when (ex is RedisServerException || ex is RedisTimeoutException || ex is RedisCommandException || ex is RedisConnectionException || ex is RedisException)
            {
                hasError = true;
                logger.Error($"ListLeftPushPublishInTransactionAsync key={key} val={val} publishChannel={publishChannel} publishValue={publishValue} failed", ex);
                throw;
            }
        }

        public async Task<bool> ListLeftPushPublishInTransactionAsync(RedisKey key, IEnumerable<MyMessage> mms, RedisChannel publishChannel, RedisValue publishValue)
        {
            try
            {
                if (logger.IsDebugEnabled)
                {
                    logger.Debug($"ListLeftPushPublishInTransactionAsync key={key} mms={mms.Count()} publishChannel={publishChannel} publishValue={publishValue}");
                }
                var transaction = Database.CreateTransaction();
                #pragma warning disable CS4014
                foreach (var mm in mms)
                {
                    var str = mm.ToShortString() ?? string.Empty;
                    transaction.ListLeftPushAsync(key, str, When.Always, DefaultCommandFlags);
                }
                transaction.PublishAsync(publishChannel, publishValue, DefaultCommandFlags);
                #pragma warning restore CS4014

                var response = await transaction.ExecuteAsync(DefaultCommandFlags);
                hasError = false;
                return response;
            }
            catch (Exception ex) when (ex is RedisServerException || ex is RedisTimeoutException || ex is RedisCommandException || ex is RedisConnectionException || ex is RedisException)
            {
                hasError = true;
                var mmsSeqIds = mms.Select(mm => mm.Sequence);
                var strIds = string.Join(",", mmsSeqIds);
                logger.Error($"ListLeftPushPublishInTransactionAsync key={key} seqIds={strIds} publishChannel={publishChannel} publishValue={publishValue} failed", ex);
                throw;
            }
        }

        public async Task<long> ListRemoveAsync(RedisKey key, RedisValue val)
        {
            try
            {
                if (logger.IsDebugEnabled)
                {
                    logger.Debug($"ListRemoveAsync key={key} value={val}");
                }
                var response = await Database.ListRemoveAsync(key, val, count: 0, DefaultCommandFlags);
                hasError = false;
                return response;
            }
            catch (Exception ex) when (ex is RedisServerException || ex is RedisTimeoutException || ex is RedisCommandException || ex is RedisConnectionException || ex is RedisException)
            {
                hasError = true;
                logger.Error($"ListRemoveAsync key={key} value={val} failed", ex);
                throw;
            }
        }

        public async Task<string> ListRightPopAsync(RedisKey key)
        {
            try
            {
                if (logger.IsDebugEnabled)
                {
                    logger.Debug($"ListRightPopAsync key={key}");
                }
                var response = await Database.ListRightPopAsync(key, DefaultCommandFlags);
                hasError = false;
                return response.IsNull ? string.Empty : (response.ToString() ?? string.Empty);
            }
            catch (Exception ex) when (ex is RedisServerException || ex is RedisTimeoutException || ex is RedisCommandException || ex is RedisConnectionException || ex is RedisException)
            {
                hasError = true;
                logger.Error($"ListRightPopAsync key={key} failed", ex);
                throw;
            }
        }

        private const string StreamAddListRemovePublishLuaScript = @"
            local removeCount = redis.call('LREM', KEYS[1], ARGV[1], ARGV[2])
            if tonumber(removeCount) > 0 then
                local streamData = cjson.decode(ARGV[3])
                local streamArgs = {}
                for key, value in pairs(streamData) do
                    table.insert(streamArgs, key)
                    table.insert(streamArgs, tostring(value))
                end
                redis.call('XADD', KEYS[2], '*', unpack(streamArgs))
                redis.call('PUBLISH', KEYS[3], ARGV[4])
            end
            return removeCount
        ";

        public async Task<bool> StreamAddListRemovePublishInTransactionAsync(RedisKey streamAddKey, RedisValue streamAddVal, string listRemoveKey, RedisValue listRemoveVal, RedisChannel publishChannel, RedisValue publishValue)
        {
            try
            {
                if (logger.IsDebugEnabled)
                {
                    logger.Debug($"StreamAddListRemovePublishInTransactionAsync streamAddKey={streamAddKey} streamAddVal={streamAddVal} listRemoveKey={listRemoveKey} listRemoveVal={listRemoveVal} publishChannel={publishChannel} publishValue={publishValue}");
                }
                var keys = new RedisKey[] { listRemoveKey, streamAddKey, publishChannel.ToString() };
                var vals = new RedisValue[] { 0, listRemoveVal, streamAddVal, publishValue };
                var response = await Database.ScriptEvaluateAsync(StreamAddListRemovePublishLuaScript, keys, vals, DefaultCommandFlags);
                hasError = false;
                return (long)response > 0;
            }
            catch (Exception ex) when (ex is RedisServerException || ex is RedisTimeoutException || ex is RedisCommandException || ex is RedisConnectionException || ex is RedisException)
            {
                hasError = true;
                logger.Error($"StreamAddListRemovePublishInTransactionAsync with streamAddKey={streamAddKey} streamAddVal={streamAddVal} listRemoveKey={listRemoveKey} listRemoveVal={listRemoveVal} publishChannel={publishChannel} publishValue={publishValue} failed", ex);
                throw;
            }
        }

        public async Task<bool> KeyExistsAsync(RedisKey key)
        {
            try
            {
                if (logger.IsDebugEnabled)
                {
                    logger.Debug($"KeyExistsAsync key={key}");
                }
                var response = await Database.KeyExistsAsync(key, DefaultCommandFlags);
                hasError = false;
                return response;
            }
            catch (Exception ex) when (ex is RedisServerException || ex is RedisTimeoutException || ex is RedisCommandException || ex is RedisConnectionException || ex is RedisException)
            {
                hasError = true;
                logger.Error($"KeyExistsAsync key={key} failed", ex);
                throw;
            }
        }

        public async Task<StreamInfo> StreamInfoAsync(RedisKey key)
        {
            try
            {
                if (logger.IsDebugEnabled)
                {
                    logger.Debug($"StreamInfoAsync key={key}");
                }
                var response = await Database.StreamInfoAsync(key, DefaultCommandFlags);
                hasError = false;
                return response;
            }
            catch (Exception ex) when (ex is RedisServerException || ex is RedisTimeoutException || ex is RedisCommandException || ex is RedisConnectionException || ex is RedisException)
            {
                hasError = true;
                logger.Error($"StreamInfoAsync key={key} failed", ex);
                throw;
            }
        }

        private const string StreamAddListLeftPushStreamDeletePublishInTransactionScript = @"
local streamAddKey = KEYS[1]
local listLeftPushKey = KEYS[2]
local streamDeleteKey = KEYS[3]
local publishChannel = KEYS[4]

local valuesCount = tonumber(ARGV[1])
local publishValue = ARGV[2]

-- Process StreamAdd and ListLeftPush
for i = 1, valuesCount do
    local listValue = ARGV[2 + (i-1) * 2 + 1]
    local streamEntries = cjson.decode(ARGV[2 + (i-1) * 2 + 2])
    local args = {'*'}
    for k, v in pairs(streamEntries) do
        table.insert(args, k)
        table.insert(args, v)
    end
    redis.call('XADD', streamAddKey, unpack(args))
    redis.call('LPUSH', listLeftPushKey, listValue)
end

-- Process StreamDelete
local deleteStartIndex = 3 + valuesCount * 2
local deleteCount = tonumber(ARGV[deleteStartIndex])
for i = 1, deleteCount do
    redis.call('XDEL', streamDeleteKey, ARGV[deleteStartIndex + i])
end

-- Publish
redis.call('PUBLISH', publishChannel, publishValue)

return 'OK'";

        public async Task<bool> StreamAddListLeftPushStreamDeletePublishInTransactionAsync(RedisKey streamAddKey, RedisKey listLeftPushKey, List<Tuple<string, NameValueEntry[]>> values, RedisKey streamDeleteKey, RedisValue[] streamDeleteValues, RedisChannel publishChannel, RedisValue publishValue)
        {
            try
            {
                if (logger.IsDebugEnabled)
                {
                    logger.Debug($"StreamAddListLeftPushStreamDeletePublishInTransactionAsync streamAddKey={streamAddKey} listLeftPushKey={listLeftPushKey} values={values.Count} streamDeleteKey={streamDeleteKey} streamDeleteValues={streamDeleteValues.Length} publishChannel={publishChannel} publishValue={publishValue}");
                }                

                var keys = new RedisKey[] { streamAddKey, listLeftPushKey, streamDeleteKey, publishChannel.ToString() };
                var args = new List<RedisValue> { values.Count, publishValue };
                foreach (var tuple in values)
                {
                    args.Add(tuple.Item1);
                    args.Add(JsonConvert.SerializeObject(tuple.Item2.ToDictionary(x => x.Name, x => x.Value)));
                }
                args.Add(streamDeleteValues.Length);
                args.AddRange(streamDeleteValues);

                var result = await Database.ScriptEvaluateAsync(StreamAddListLeftPushStreamDeletePublishInTransactionScript, keys, args.ToArray(), DefaultCommandFlags);
                hasError = false;
                return result.ToString() == "OK";
            }
            catch (Exception ex) when (ex is RedisServerException || ex is RedisTimeoutException || ex is RedisCommandException || ex is RedisConnectionException || ex is RedisException)
            {
                hasError = true;
                var tuples = values.Select(t => $"{t.Item1}:{t.Item2}");
                var valStr = string.Join(",", tuples);
                var rvs = streamDeleteValues.Select(v => v.IsNull ? "<null>" : v.ToString() ?? string.Empty);
                var rvsStr = string.Join(",", rvs);
                logger.Error($"StreamAddListLeftPushStreamDeletePublishInTransactionAsync streamAddKey={streamAddKey} listLeftPushKey={listLeftPushKey} values={valStr} streamDeleteKey={streamDeleteKey} streamDeleteValues={rvsStr} publishChannel={publishChannel} publishValue={publishValue} failed", ex);
                throw;
            }
        }

        public async Task<bool> StringSetAsync(RedisKey key, RedisValue val, TimeSpan? expiry, When when)
        {
            try
            {
                if (logger.IsDebugEnabled)
                {
                    logger.Debug($"StringSetAsync key={key} val={val} expiry={expiry} when={when}");
                }
                var response = await Database.StringSetAsync(key, val, expiry, when, DefaultCommandFlags);
                hasError = false;
                return response;
            }
            catch (Exception ex) when (ex is RedisServerException || ex is RedisTimeoutException || ex is RedisCommandException || ex is RedisConnectionException || ex is RedisException)
            {
                hasError = true;
                logger.Error($"StringSetAsync key={key} val={val} expiry={expiry} when={when} failed", ex);
                throw;
            }
        }

        public async Task<RedisValue> StringGetAsync(RedisKey key)
        {
            try
            {
                if (logger.IsDebugEnabled)
                {
                    logger.Debug($"StringGetAsync key={key}");
                }
                var response = await Database.StringGetAsync(key, DefaultCommandFlags);
                hasError = false;
                return response;
            }
            catch (Exception ex) when (ex is RedisServerException || ex is RedisTimeoutException || ex is RedisCommandException || ex is RedisConnectionException || ex is RedisException)
            {
                hasError = true;
                logger.Error($"StringGetAsync key={key} failed", ex);
                throw;
            }
        }

        public async Task<RedisResult> ScriptEvaluateAsync(string scriptName, string script, RedisKey[]? keys, RedisValue[]? values)
        {
            try
            {
                if (logger.IsDebugEnabled)
                {
                    var strKeys = keys == null ? string.Empty : string.Join(",", keys.Select(x => x.ToString()));
                    var strVals = values == null ? string.Empty : string.Join(",", values.Select(x => x.ToString()));
                    logger.Debug($"ScriptEvaluateAsync script={scriptName} keys={strKeys} values={strVals}");
                }
                var response = await Database.ScriptEvaluateAsync(script, keys, values, DefaultCommandFlags);
                hasError = false;
                return response;
            }
            catch (Exception ex) when (ex is RedisServerException || ex is RedisTimeoutException || ex is RedisCommandException || ex is RedisConnectionException || ex is RedisException)
            {
                hasError = true;
                var scriptOneLine = script.Replace("\n", "<nl> ").Replace("\r", "<cr> ").Replace("\t", "<tab>");
                var keysArr = keys?.Select(x => x.ToString()) ?? [];
                var valsArr = values?.Select(x => x.ToString()) ?? [];
                var keysStr = string.Join(",", keysArr);
                var valsStr = string.Join(",", valsArr);
                logger.Error($"ScriptEvaluateAsync script={scriptOneLine} keys={keysStr} values={valsStr} failed", ex);
                throw;
            }
        }

        public async Task<bool> LockTakeAsync(RedisKey key, RedisValue val, TimeSpan expiry)
        {
            try
            {
                if (logger.IsDebugEnabled)
                {
                    logger.Debug($"LockTakeAsync key={key} val={val} expiry={expiry}");
                }
                var response = await Database.LockTakeAsync(key, val, expiry, DefaultCommandFlags);
                hasError = false;
                return response;
            }
            catch (Exception ex) when (ex is RedisServerException || ex is RedisTimeoutException || ex is RedisCommandException || ex is RedisConnectionException || ex is RedisException)
            {
                hasError = true;
                logger.Error($"LockTakeAsync key={key} val={val} expiry={expiry} failed", ex);
                throw;
            }
        }
        public async Task<bool> LockExtendAsync(RedisKey key, RedisValue val, TimeSpan expiry)
        {
            try
            {
                if (logger.IsDebugEnabled)
                {
                    logger.Debug($"LockExtendAsync key={key} val={val} expiry={expiry}");
                }
                var response = await Database.LockExtendAsync(key, val, expiry, DefaultCommandFlags);
                hasError = false;
                return response;
            }
            catch (Exception ex) when (ex is RedisServerException || ex is RedisTimeoutException || ex is RedisCommandException || ex is RedisConnectionException || ex is RedisException)
            {
                hasError = true;
                logger.Error($"LockExtendAsync key={key} val={val} expiry={expiry} failed", ex);
                throw;
            }
        }
        public async Task<RedisValue> LockQueryAsync(RedisKey key)
        {
            try
            {
                if (logger.IsDebugEnabled)
                {
                    logger.Debug($"LockQueryAsync key={key}");
                }
                var response = await Database.LockQueryAsync(key, DefaultCommandFlags);
                hasError = false;
                return response;
            }
            catch (Exception ex) when (ex is RedisServerException || ex is RedisTimeoutException || ex is RedisCommandException || ex is RedisConnectionException || ex is RedisException)
            {
                hasError = true;
                logger.Error($"LockQueryAsync key={key} failed", ex);
                throw;
            }
        }
        public async Task<bool> LockReleaseAsync(RedisKey key, RedisValue val)
        {
            try
            {
                if (logger.IsDebugEnabled)
                {
                    logger.Debug($"LockReleaseAsync key={key} val={val}");
                }
                var response = await Database.LockReleaseAsync(key, val, DefaultCommandFlags);
                hasError = false;
                return response;
            }
            catch (Exception ex) when (ex is RedisServerException || ex is RedisTimeoutException || ex is RedisCommandException || ex is RedisConnectionException || ex is RedisException)
            {
                hasError = true;
                logger.Error($"LockReleaseAsync key={key} val={val} failed", ex);
                throw;
            }
        }

        public async Task<long> GetListMessagesCount(RedisKey key)
        {
            try
            {
                if (logger.IsDebugEnabled)
                {
                    logger.Debug($"GetListMessagesCount key={key}");
                }
                var response = await Database.ListLengthAsync(key, DefaultCommandFlags);
                hasError = false;
                return response;
            }
            catch (Exception ex) when (ex is RedisServerException || ex is RedisTimeoutException || ex is RedisCommandException || ex is RedisConnectionException || ex is RedisException)
            {
                hasError = true;
                logger.Error($"GetListMessagesCount key={key} failed", ex);
                throw;
            }
        }

        public async Task<long> GetStreamMessagesCount(RedisKey key)
        {
            try
            {
                if (logger.IsDebugEnabled)
                {
                    logger.Debug($"GetStreamMessagesCount key={key}");
                }
                var response = await Database.StreamLengthAsync(key, DefaultCommandFlags);
                hasError = false;
                return response;
            }
            catch (Exception ex) when (ex is RedisServerException || ex is RedisTimeoutException || ex is RedisCommandException || ex is RedisConnectionException || ex is RedisException)
            {
                hasError = true;
                logger.Error($"GetStreamMessagesCount key={key} failed", ex);
                throw;
            }
        }

        public async Task<MyMessage?> GetStreamLastMessage(RedisKey key)
        {
            try
            {
                if (logger.IsDebugEnabled)
                {
                    logger.Debug($"GetStreamLastMessage key={key}");
                }
                var entries = await Database.StreamRangeAsync(key: key, minId: "-", maxId: "+", count: 1, messageOrder: Order.Descending, DefaultCommandFlags);
                hasError = false;
                return entries.Length > 0 ? entries[0].ToMyMessage() : null;
            }
            catch (Exception ex) when (ex is RedisServerException || ex is RedisTimeoutException || ex is RedisCommandException || ex is RedisConnectionException || ex is RedisException)
            {
                hasError = true;
                logger.Error($"GetStreamLastMessage key={key} failed", ex);
                throw;
            }
        }

        public async Task<string> ServerInfos()
        {
            var servers = Connection.GetServers();
            var arr = new List<string>();
            foreach (var server in servers)
            {
                //var info = server.Info("commandstats");
                var infos = await server.InfoAsync();
                foreach (var info in infos)
                {
                    if (info == null)
                        continue;

                    foreach(var i in info)
                    {
                        arr.Add($"{info.Key}: {i.Key}={i.Value}");
                    }
                }

                var lists = await server.CommandListAsync();
                foreach (var list in lists)
                {
                    arr.Add($"CommandList: {list}");
                }
                //var keys = server.CommandGetKeys();
                //var keys = server.CommandCount();
            }
            var str = string.Join("\n", arr);
            logger.Info(str);
            return str;
        }

        public async Task<TimeSpan> Ping()
        {
            try
            {
                if (logger.IsDebugEnabled)
                {
                    logger.Debug($"Ping when isConnected={isConnected} and Connection.IsConnected={Connection.IsConnected}");
                }
                var response = await Database.PingAsync(DefaultCommandFlags);
                hasError = false;
                return response;
            }
            catch (Exception ex) when (ex is RedisServerException || ex is RedisTimeoutException || ex is RedisCommandException || ex is RedisConnectionException || ex is RedisException)
            {
                hasError = true;
                logger.Error($"Ping failed", ex);
                throw;
            }
        }
        
        public void Dispose()
        {
            if (isDisposed)
            {
                logger.Warn($"Connection already disposed, skipping");
            }
            else
            {
                logger.Debug($"Disposing connection");
                Connection.Dispose();
                isDisposed = true;
            }
        }
    }
}