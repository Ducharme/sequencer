using CommonTypes;
using log4net;
using StackExchange.Redis;

namespace RedisAccessLayer
{
    public class RedisConnectionManager : IRedisConnectionManager, IDisposable
    {
        public IConnectionMultiplexer Connection { get; private set; }
        public IDatabase Database { get; private set; }
        public ISubscriber Subscriber { get; private set; }

        public readonly IRedisConfigurationFetcher ConfigurationFetcher;
        public readonly IConnectionMultiplexerWrapper ConnectionMultiplexerWrapper;
        private readonly Dictionary<RedisChannel, Action<RedisChannel, RedisValue>> subscriptions = [];
        protected const CommandFlags DefaultCommandFlags = CommandFlags.DemandMaster;

        protected volatile bool isConnected = false;
        protected volatile bool hasError = false;

        private static readonly ILog logger = LogManager.GetLogger(typeof(RedisConnectionManager));

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

        public RedisConnectionManager(IRedisConfigurationFetcher cf, IConnectionMultiplexerWrapper cm)
        {
            ConfigurationFetcher = cf;
            ConnectionMultiplexerWrapper = cm;

            logger.Info($"Creating connection");
            Connection = cm.Connect(cf.OptionsWrapper);
            Database = Connection.GetDatabase();
            Subscriber = Connection.GetSubscriber();

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
                Connection = ConnectionMultiplexerWrapper.Connect(ConfigurationFetcher.OptionsWrapper);
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

        public async Task<string> ListGetByIndexAsync(RedisKey key)
        {
            try
            {
                var response = await Database.ListGetByIndexAsync(key, 0, DefaultCommandFlags);
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

        public async Task<bool> ListLeftPushPublishInTransactionAsync(RedisKey key, RedisValue val, RedisChannel publishChannel, RedisValue publishValue)
        {
            try
            {
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

        public async Task<bool> StreamAddListLeftPushStreamDeletePublishInTransactionAsync(RedisKey streamAddKey, RedisKey listLeftPushKey, List<Tuple<string, NameValueEntry[]>> values, RedisKey streamDeleteKey, RedisValue[] streamDeleteValues, RedisChannel publishChannel, RedisValue publishValue)
        {
            try
            {
                var transaction = Database.CreateTransaction();
                #pragma warning disable CS4014
                foreach (var tuple in values)
                {
                    transaction.StreamAddAsync(streamAddKey, tuple.Item2);
                    transaction.ListLeftPushAsync(listLeftPushKey, tuple.Item1);
                }
                transaction.StreamDeleteAsync(streamDeleteKey, streamDeleteValues);
                transaction.PublishAsync(publishChannel, publishValue);
                #pragma warning restore CS4014

                var response = await transaction.ExecuteAsync(DefaultCommandFlags);
                hasError = false;
                return response;
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

        public async Task<RedisResult> ScriptEvaluateAsync(string script, RedisKey[]? keys, RedisValue[]? values)
        {
            try
            {
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

        public void Dispose()
        {
            Connection.Dispose();
        }
    }
}