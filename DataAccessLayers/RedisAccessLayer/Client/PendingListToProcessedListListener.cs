//#define LIST_CONTAINS

using CommonTypes;

using log4net;
using StackExchange.Redis;

namespace RedisAccessLayer
{
    public class PendingListToProcessedListListener : PendingListToProcessedStreamClientBase, IPendingToProcessedListener
    {
        public string? LastProcessedEntryId { get; protected set; }
        private long shouldListen = 1;
        private long pendingMessages = 0;
        private const long ListBatchSize = 1000;
        private readonly ManualResetEventSlim newMessageEvent = new ManualResetEventSlim(false);
        private const int WaitTime = 100;
        private const int LongWaitTime = 1000;
        private const int ProcessingStuckCheckFrequency = 3000;
        private const int ProcessingAllowedTime = 1001;
        private readonly RedisChannel PendingNewMessagesChannel;
        private readonly RedisChannel ProcessedStreamChannel;
        private static readonly ILog logger = LogManager.GetLogger(typeof(PendingListToProcessedListListener));

        public PendingListToProcessedListListener(IRedisConnectionManager cm)
            : base (cm)
        {
            var channelPrefix = EnvVarReader.GetString("REDIS_CHANNEL_PREFIX", EnvVarReader.NotFound);
            var prefix = channelPrefix != EnvVarReader.NotFound ? string.Concat("{", channelPrefix, "}:") : string.Empty;
            PendingNewMessagesChannel = RedisChannel.Literal(prefix + "NewMessages");
            ProcessedStreamChannel = RedisChannel.Literal(prefix + "ProcessedMessages");
        }

        public void StopListening()
        {
            Interlocked.Exchange(ref this.shouldListen, 0);
        }

        public async Task ListenMessagesFromPendingList(Func<string, string, MyMessage, Task<bool>> handler)
        {
            await SubscribeToNewMessagesChannel();

            var noValueCount = 0;
            var lastReadHadvalue = false;
            var lastProcessingStuckCheck = DateTime.MinValue;
            var listen = Interlocked.Read(ref this.shouldListen);
            while (listen == 1)
            {
                try
                {
                    // Monitor processingListKey and try to reprocess in case work could not be completed successfully
                    if (DateTime.UtcNow.Subtract(lastProcessingStuckCheck) > TimeSpan.FromMilliseconds(ProcessingStuckCheckFrequency))
                    {
                        logger.Debug($"Checking if any mesasge is stuck in {processingListKey}");
                        var lst = await rcm.ListRangeAsync(processingListKey);
                        logger.Debug($"List {processingListKey} contains {lst.Length} messages");
                        if (lst.Length > 0)
                        {
                            foreach(var rv1 in lst)
                            {
                                if (rv1.IsNullOrEmpty)
                                {
                                    continue;
                                }

                                var str1 = rv1.ToString();
                                if (!string.IsNullOrEmpty(str1))
                                {
                                    var mm1 = str1.FromShortString();
                                    if (mm1 != null)
                                    {
                                        var processingAt1 = DateTimeHelper.GetDateTime(mm1.ProcessingAt);
                                        var allowedTime = TimeSpan.FromMilliseconds(ProcessingAllowedTime);
                                        var diffTime = DateTime.UtcNow.Subtract(processingAt1);
                                        logger.Warn($"List {processingListKey} has message {rv1} with processingAt={processingAt1} and diffTime(ms)={diffTime.TotalMilliseconds}");
                                        if (diffTime > allowedTime)
                                        {
                                            logger.Warn($"List {processingListKey} is stuck with message {rv1}, will try reprocessing");
                                            await handler(pendingListKey, str1, mm1);
                                        } else {
                                            logger.Debug($"List {processingListKey} message {rv1} is not old enough to be reprocessed yet");
                                        }
                                    }
                                }
                            }
                        }
                        lastProcessingStuckCheck = DateTime.UtcNow;
                    }

                    var count = Interlocked.Read(ref pendingMessages);
                    var shoudWait = count == 0 && !lastReadHadvalue;
                    if (shoudWait)
                    {
                        newMessageEvent.Wait(WaitTime);
                    }

                    newMessageEvent.Reset();
                    var dtStart = DateTime.Now;
                    var processingAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    var str = await rcm.ListRightPopLeftPushListSetByIndexInTransactionAsync(pendingListKey, processingListKey, processingAt);
                    var dtEnd = DateTime.Now;
                    if (!string.IsNullOrEmpty(str))
                    {
                        lastReadHadvalue = true;
                        MyMessage? mm = str.FromShortString();
                        if (mm != null)
                        {
                            var elapsed = Math.Round(dtEnd.Subtract(dtStart).TotalMilliseconds, 2);
                            logger.Info($"List {pendingListKey} contains message {str}, noValueCount={noValueCount}, elapsed={elapsed} ms");
                            await handler(pendingListKey, str, mm); // NOTE: Processing could be done on another thread
                        } else {
                            logger.Warn($"List {pendingListKey} contains invalid message {str}, noValueCount={noValueCount}");
                            logger.Debug($"List {processingListKey} is removing value {str}");
                            var ret = await rcm.ListRemoveAsync(processingListKey, str);
                            logger.Debug($"List {processingListKey} removed value {str}, count={ret}");
                        }

                        noValueCount = 0;
                    } else {
                        logger.Debug($"ListRightPopLeftPushListSetByIndexInTransactionAsync received an empty response (shoudWait={shoudWait}, noValueCount={noValueCount}, pendingMessages={pendingMessages}, lastReadHadvalue={lastReadHadvalue})");
                        lastReadHadvalue = false;
                        Interlocked.Exchange(ref pendingMessages, 0);
                        noValueCount += 1;
                    }

                    listen = Interlocked.Read(ref this.shouldListen);
                }
                catch (RedisTimeoutException)
                {
                    // https://stackexchange.github.io/StackExchange.Redis/Timeouts
                    logger.Warn("Redis client encountered an error while listening");
                    await Task.Delay(LongWaitTime);
                    await rcm.Reconnect();
                }
                catch (Exception ex) when (ex is RedisServerException || ex is RedisTimeoutException || ex is RedisConnectionException || ex is RedisException) // Error with RedisCommandException should exit
                {
                    logger.Warn("Redis client encountered an error while listening");
                    await Task.Delay(LongWaitTime);
                }
            }
        }

        private async Task SubscribeToNewMessagesChannel()
        {
            rcm.AddSubscription(PendingNewMessagesChannel, SubscribeToNewMessagesChannelHandler);

            var listen = Interlocked.Read(ref this.shouldListen);
            var subscribed = false;
            while (listen == 1 && !subscribed)
            {
                try
                {
                    await rcm.SubscribeAsync();
                    subscribed = true;
                }
                catch (RedisTimeoutException)
                {
                    // https://stackexchange.github.io/StackExchange.Redis/Timeouts
                    logger.Warn("Redis client encountered an error while subscribing to new messages");
                    await Task.Delay(LongWaitTime);
                    await rcm.Reconnect();
                }
                catch (Exception ex) when (ex is RedisServerException || ex is RedisTimeoutException || ex is RedisConnectionException || ex is RedisException)
                {
                    logger.Warn("Redis client encountered an error while subscribing to new messages");
                    await Task.Delay(LongWaitTime);
                }
                finally
                {
                    listen = Interlocked.Read(ref this.shouldListen);
                }
            }
        }

        private void SubscribeToNewMessagesChannelHandler(RedisChannel channel, RedisValue message)
        {
            var chn = channel.ToString();
            var msg = message.ToString() ?? string.Empty;
            var nb = int.TryParse(msg, out int cnt) ? cnt : 1;

            logger.Info($"Received message {message} from channel {channel}");
            Interlocked.Add(ref pendingMessages, nb);
            newMessageEvent.Set(); // Signal that a new message has been received
        }

        public async Task<bool> AppendToProcessedStreamThenDeleteFromProcessingList(MyMessage mm, string message)
        {
            var rv = new RedisValue(message);
            var committed = await rcm.StreamAddListRemovePublishInTransactionAsync(processedStreamKey, mm.ToJson(), processingListKey, rv, ProcessedStreamChannel, rv);
            if (committed)
            {
                logger.Info($"Stream {processedStreamKey} appended values {mm} then list {processingListKey} removed value {rv} in a transaction");
            }
            else
            {
                logger.Warn($"Stream {processedStreamKey} failed to append values {mm} then list {processingListKey} failed to remove value {rv} in a transaction");
            }
            return committed;
        }

        public async Task<bool> CanMessageBeProcessed(string name, long sequence)
        {
            // #1 pendingListKey is not relevant yet
            // #2 processingListKey already contains the item
            // #3 processedStreamKey entryId is deleted in the transaction
            // #4 sequencedStreamKey and #5 sequencedListKey are also updated in the transaction
            // NOTE: Not supposed to end in both pendingListKey and processingListKey
            // NOTE: List is easier and faster to check than Stream in batches
            
            var processed = await WasProcessed(sequencedListKey, sequence);
            return !processed;
            
            // Alternative (more expensive to run because multiple values are fetched many times)
            // return !await ListContains(sequencedListKey, sequence);
        }

        private async Task<bool> WasProcessed(string listKey, long sequence)
        {
            // NOTE: New values are added to the left of the list (beginning)
            // The latest value is the highest at index 0, if the list is not empty
            var val = await rcm.ListGetByIndexAsync(listKey);
            if (!string.IsNullOrEmpty(val))
            {
                if (val.Length == 0)
                {
                    return false;
                }
                else
                {
                    var str = val.ToString();
                    var indexOf = str.IndexOf(';');
                    if (indexOf > 0)
                    {
                        var lastSeqStr = str.Substring(0, indexOf);
                        if (int.TryParse(lastSeqStr, out int lastSeq))
                        {
                            return lastSeq >= sequence;
                        }
                    }
                }
            }
            return false;
        }

#if LIST_CONTAINS
        private async Task<bool> ListContains(string listKey, long sequence)
        {
            // NOTE: New values are added to the left of the list (beginning)
            var seqStr = sequence.ToString();
            var listLength = await rcm.ListLengthAsync(listKey);
            if (listLength == 0)
            {
                return false;
            }

            var batchSize = listLength < ListBatchSize ? listLength : ListBatchSize;
            long start = 0;
            long stop = batchSize-1;
            while (true)
            {
                var listValues = await rcm.ListRangeAsync(listKey, start, stop);
                var listString = listValues.Select(v => v.ToString());

                #pragma warning disable CS8619
                var firstOrDefault = listString.FirstOrDefault(v => v.StartsWith(seqStr) && v[seqStr.Length] == ';');
                #pragma warning restore CS8619
                if (firstOrDefault != null)
                {
                    return true;
                }

                start = stop + 1;
                var batchStop = start + batchSize-1;
                stop = batchStop > listLength ? listLength : batchStop;
                if (start >= listLength)
                {
                    return false;
                }
            }
        }
#endif
    }
}