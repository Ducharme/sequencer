using CommonTypes;

using log4net;
using StackExchange.Redis;

namespace RedisAccessLayer
{
    public class ProcessedListToSequencedListListener : ProcessedStreamToSequencedListClientBase, IProcessedToSequencedListener
    {
        public string? LastProcessedEntryId { get; protected set; }
        private long shouldListen = 1;
        private long pendingMessages = 0;
        private bool? isQueueEmpty = null;
        private readonly ISyncLock syncLock;
        private readonly ManualResetEventSlim newMessageEvent = new ManualResetEventSlim(false);
        private enum SequencingStatus { Succeeded, Failed, WasEmpty, Started, Skipped }
        private SequencingStatus sequencingStatus = SequencingStatus.Started;
        
        private static readonly ILog logger = LogManager.GetLogger(typeof(ProcessedListToSequencedListListener));
        private const string Dash = "-";
        private const string Dot = ".";
        private const int BufferTime = 100;
        private const int WaitTime = 100;
        private const int LongWaitTime = 1000;

        private readonly RedisChannel ProcessedStreamChannel;
        private readonly RedisChannel SequencedStreamChannel;

        public ProcessedListToSequencedListListener(IRedisConnectionManager cm, ISyncLock sl)
            : base(cm)
        {
            syncLock = sl;

            var channelPrefix = EnvVarReader.GetString("REDIS_CHANNEL_PREFIX", EnvVarReader.NotFound);
            var prefix = channelPrefix != EnvVarReader.NotFound ? string.Concat("{", channelPrefix, "}:") : string.Empty;
            SequencedStreamChannel = RedisChannel.Literal(prefix + "HighestEntryIdAndSequence");
            ProcessedStreamChannel = RedisChannel.Literal(prefix + "ProcessedMessages");
        }

        public void StopListening()
        {
            Interlocked.Exchange(ref this.shouldListen, 0);
        }

        public async Task ListenForPendingMessages(Func<Dictionary<string, MyMessage>, Task<bool>> handler)
        {
            rcm.AddSubscription(ProcessedStreamChannel, SubscribeToProcessedChannelHandler);
            rcm.AddSubscription(SequencedStreamChannel, SubscribeToSequencedChannelHandler);

            var subscribed = false;
            var listen = Interlocked.Read(ref this.shouldListen);
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
                    logger.Warn("Redis client encountered an error while subscribing");
                    await Task.Delay(LongWaitTime);
                    await rcm.Reconnect();
                }
                catch (Exception ex) when (ex is RedisServerException || ex is RedisTimeoutException || ex is RedisConnectionException || ex is RedisException)
                {
                    logger.Warn("Redis client encountered an error while subscribing");
                    await Task.Delay(LongWaitTime);
                }
                finally
                {
                    listen = Interlocked.Read(ref this.shouldListen);
                }
            }

            bool? isLeader = null;
            var lastEntries = Array.Empty<string>();
            var defaultLockTime = syncLock.LockExpiry;
            var lastLeaderAttempt = DateTime.MinValue;
            var releaseLock = false;
            while (listen == 1)
            {
                try
                {
                   // Acquire lock to ensure a single sequencer works at a specific time
                    bool acquired;
                    if (!isLeader.HasValue)
                    {
                        acquired = await syncLock.AcquireLock();
                    }
                    else if (isLeader.Value)
                    {
                        var remainingTime = syncLock.RemainingLockTime;
                        acquired = remainingTime > TimeSpan.Zero;
                        if (remainingTime < TimeSpan.FromMilliseconds(BufferTime))
                        {
                            var extended = await syncLock.ExtendLock(defaultLockTime);
                            if (!extended)
                            {
                                releaseLock = true;
                            }
                        }
                    }
                    else
                    {
                        if (DateTime.UtcNow.Subtract(lastLeaderAttempt) > TimeSpan.FromMilliseconds(BufferTime))
                        {
                            acquired = await syncLock.AcquireLock();
                            lastLeaderAttempt = DateTime.UtcNow;
                        }
                        else
                        {
                            acquired = false;
                        }
                    }

                    if (!isLeader.HasValue || isLeader.Value != acquired)
                    {
                        logger.Debug($"AcquireLock={acquired}");
                    }

                    if (acquired == true)
                    {
                        isLeader = true;
                        if (string.IsNullOrEmpty(LastProcessedEntryId))
                        {
                            logger.Debug("Set last values explicitly to initialize");
                            await SetLastMessageFromSequencedStream();
                        }

                        var count = Interlocked.Read(ref pendingMessages);
                        if (count == 0 && sequencingStatus == SequencingStatus.WasEmpty)
                        {
                            newMessageEvent.Wait(WaitTime);
                        }

                        newMessageEvent.Reset();
                        var entries = await rcm.StreamReadAsync(processedStreamKey, LastProcessedEntryId);
                        if (entries.Length > 0)
                        {
                            isQueueEmpty = false;
                            lastEntries = await SequenceEntries(handler, lastEntries, entries);
                        }
                        else
                        {
                            if (isQueueEmpty == null)
                            {
                                isQueueEmpty = true;
                                logger.Info($"Stream {this.processedStreamKey} is empty");
                            }
                            sequencingStatus = SequencingStatus.WasEmpty;
                            Interlocked.Exchange(ref pendingMessages, 0);
                            lastEntries = [];
                        }

                        if (releaseLock)
                        {
                            var released = await syncLock.ReleaseLock();
                            if (released.HasValue && released.Value)
                            {
                                releaseLock = false;
                                isLeader = false;
                            }
                        }
                    }
                    else
                    {
                        isLeader = false;
                        await Task.Delay(WaitTime);
                    }
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
                finally
                {
                    listen = Interlocked.Read(ref this.shouldListen);
                }
            }
        }

        private async Task<string[]> SequenceEntries(Func<Dictionary<string, MyMessage>, Task<bool>> handler, string[] lastEntries, StreamEntry[] entries)
        {
            var thisEntries = entries.Select(e => e.Id.ToString()).ToArray();
            if (!thisEntries.SequenceEqual(lastEntries))
            {
                logger.Info($"Received new stream ids {string.Join(",", thisEntries)}");
                lastEntries = thisEntries;

                var dic = entries.ToDictionary(e => e.Id.ToString(), e => e.ToMyMessage());
                long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                foreach (var kvp in dic)
                {
                    kvp.Value.SequencingAt = timestamp;
                }

                var ids = dic.Values.Select(kvp => kvp.Sequence).ToList();
                var sequenceComplete = SequenceHelper.IsSequenceComplete(this.LastProcessedSequenceId, ids, logger);
                var partialSequence = SequenceHelper.GetPartialSequence(this.LastProcessedSequenceId, ids, logger);
                if (logger.IsDebugEnabled)
                {
                    var idsJoined = string.Join(",", ids);
                    var idsOrderedJoined = string.Join(",", ids.Order());
                    var partialSequenceStr = partialSequence.Count > 0 ? string.Join(",", partialSequence) : "None";
                    logger.Debug($"Sequence ids are {idsJoined} (ordered:{idsOrderedJoined}). IsSequenceComplete: {sequenceComplete} & GetPartialSequence:{partialSequenceStr}");
                }

                // NOTE: Do not process if there is a missing message in the middle of the sequence of ids
                if (sequenceComplete)
                {
                    var success = await handler(dic);
                    sequencingStatus = success ? SequencingStatus.Succeeded : SequencingStatus.Failed;
                }
                else if (partialSequence.Count > 0)
                {
                    foreach (var id in ids)
                    {
                        if (!partialSequence.Contains(id))
                        {
                            var kvp = dic.First(e => e.Value.Sequence == id);
                            dic.Remove(kvp.Key);
                        }
                    }
                    var success = await handler(dic);
                    sequencingStatus = success ? SequencingStatus.Succeeded : SequencingStatus.Failed;
                }
                else
                {
                    if (ids.Count > 0)
                    {
                        logger.Debug("Set last values explicitly by precaution");
                        await SetLastMessageFromSequencedStream();
                    }
                    sequencingStatus = SequencingStatus.Skipped;
                }
            }
            else
            {
                sequencingStatus = SequencingStatus.Skipped;
            }

            return lastEntries;
        }

        private void SubscribeToProcessedChannelHandler(RedisChannel channel, RedisValue message)
        {
            var msg = message.ToString() ?? string.Empty;
            var mm = msg.FromShortString();

            if (mm == null)
            {
                logger.Error($"Received malformed message {message} from channel {channel} (MyMessage was expected)");
            }
            else 
            {
                logger.Info($"Received message {message} from channel {channel}");
                Interlocked.Increment(ref pendingMessages);
                newMessageEvent.Set(); // Signal that a new message has been received
            }
        }

        private void SubscribeToSequencedChannelHandler(RedisChannel channel, RedisValue message)
        {
            var msg = message.ToString() ?? string.Empty;

            var tokens = msg.Split(':');
            if (tokens.Length == 2)
            {
                // Make sure the Sequence and EntryId are higher than current latest
                var lastSeq = int.Parse(tokens[1]);
                var entryId = tokens[0];
                if (lastSeq > LastProcessedSequenceId)
                {
                    LastProcessedSequenceId = lastSeq;
                    LastProcessedEntryId = entryId;
                    logger.Info($"Received message {message} from channel {channel} with LastProcessedEntryId={LastProcessedEntryId} and LastProcessedSequenceId={LastProcessedSequenceId}");
                }
                else
                {
                    logger.Info($"Received message {message} from channel {channel} was discarded because its Sequence {lastSeq} is not higher than LastProcessedSequenceId {LastProcessedSequenceId}");
                }
            }
            else
            {
                logger.Error($"Received malformed message {message} from channel {channel} (2 tokens were expected)");
            }
        }

        private static Tuple<string?, long?> GetValue(StreamEntry se)
        {
            Tuple<string?, long?> tuple;
            if (!se.IsNull)
            {
                var entryIdNve = se.Values.FirstOrDefault(v => v.Name == MyMessageFieldNames.ColumnHighestEntryId);
                var seqNve = se.Values.FirstOrDefault(v => v.Name == MyMessageFieldNames.Sequence);
                string? entryId = entryIdNve == default || entryIdNve.Value.IsNullOrEmpty ? null : entryIdNve.Value.ToString();
                long? seq = seqNve == default || seqNve.Value.IsNullOrEmpty ? null : long.Parse(seqNve.Value.ToString());
                tuple = new (entryId, seq);
            }
            else
            {
                tuple = new (null, null);
            }
            return tuple;
        }

        public async Task SetLastMessageFromSequencedStream()
        {
            // Handle entryId when starting afterward (should resume to the next key not the first)
            string? lastKey = "-"; // Use "$" for last message and "-" for all messages
            var listen = Interlocked.Read(ref this.shouldListen);
            if (listen == 1)
            {
                bool exists = await rcm.KeyExistsAsync(sequencedStreamKey);
                if (exists)
                {
                    var si = await rcm.StreamInfoAsync(sequencedStreamKey);
                    var tuple = GetValue(si.LastEntry);
                    if (!string.IsNullOrEmpty(tuple.Item1) && tuple.Item2 != null)
                    {
                        LastProcessedSequenceId = tuple.Item2;
                        LastProcessedEntryId = tuple.Item1;
                        logger.Info($"Received stream info for {sequencedStreamKey} with LastProcessedEntryId={LastProcessedEntryId} and LastProcessedSequenceId={LastProcessedSequenceId}");
                    }
                    else
                    {
                        logger.Warn($"Received stream info for {sequencedStreamKey} with missing information to set LastProcessedEntryId and LastProcessedSequenceId");
                    }
                }
                else
                {
                    logger.Info($"Stream {sequencedStreamKey} does not exist, LastProcessedEntryId={LastProcessedEntryId} and LastProcessedSequenceId={LastProcessedSequenceId}");
                }
            }

            if (LastProcessedEntryId == null)
            {
                logger.Info($"Setting last processed EntryId={lastKey}");
                LastProcessedEntryId = lastKey;
            }
            if (LastProcessedSequenceId == null)
            {
                logger.Info($"Setting last processed SequenceId=0");
                LastProcessedSequenceId = 0;
            }
        }

        // Fallback approach when subscription to events handling new messages may not be working
        public async Task<KeyValuePair<string, MyMessage>> GetLastMessageFromPendingStream()
        {
            // Handle entryId when starting afterward (should resume to the next key not the first)
            string? lastKey = "$"; // Use "$" for last message and "-" for all messages
            var listen = Interlocked.Read(ref this.shouldListen);
            var kvp = default(KeyValuePair<string, MyMessage>);
            if (listen == 1)
            {
                var entries = await rcm.StreamReadAsync(processedStreamKey, lastKey);
                logger.Info($"Stream {processedStreamKey} received entries");
                if (entries != null)
                {
                    if (entries.Length > 0)
                    {
                        foreach (var entry in entries)
                        {
                            MyMessage mm = entry.ToMyMessage();
                            kvp = new KeyValuePair<string, MyMessage>(entry.Id.ToString(), mm);
                            logger.Info($"Stream {processedStreamKey} with id {entry.Id} contains values {mm}");
                        }
                    } else {
                        logger.Info($"Stream {processedStreamKey} received 0 entry");
                    }
                } else {
                    logger.Info($"Stream {processedStreamKey} received null entry");
                }
            }
            return kvp;
        }

        public async Task<Tuple<bool, string, long>> FromProcessedToSequenced(Dictionary<string, MyMessage> dic)
        {
            var entryIdsToDouble = dic.Keys.Select(key => new KeyValuePair<string, double>(key, double.Parse(key.Replace(Dash, Dot))));
            var orderedByEntryIds = entryIdsToDouble.OrderBy(kvp => kvp.Value).Select(kvp => kvp.Key).ToList();
            var highestEntryId = orderedByEntryIds.Last();
            var highestEntryIdSeq = dic[highestEntryId].Sequence;
            var orderedBySequence = dic.OrderBy(kvp => kvp.Value.Sequence);
            var allEntryIds = string.Join(", ", orderedByEntryIds);
            var highestSequence = dic.Max(kvp => kvp.Value.Sequence);

            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            foreach (var kvp in dic)
            {
                kvp.Value.SequencedAt = timestamp;
            }

            var extraKvp = new KeyValuePair<string, string>(MyMessageFieldNames.ColumnHighestEntryId, highestEntryId);
            var lst = new List<Tuple<string, NameValueEntry[]>>();
            foreach (var kvp in orderedBySequence)
            {
                var entryId = kvp.Key;
                var mm = kvp.Value;
                var nves = mm.ToNameValueEntriesWithExtraString(extraKvp);
                logger.Info($"Appending sequencing message streamEntryId {entryId} with sequence id {mm.Sequence} to {sequencedStreamKey}");

                var str = mm.ToShortString() ?? string.Empty;
                logger.Info($"Pushing sequencing message streamEntryId {entryId} with sequence id {mm.Sequence} to {sequencedListKey}");

                lst.Add(new Tuple<string, NameValueEntry[]>(str, nves));
            }

            logger.Info($"Deleting sequencing message streamEntryId {highestEntryId} with sequence id {highestEntryIdSeq} from {processedStreamKey}");
            var rvs = new RedisValue[] {highestEntryId};

            var highestValue = string.Concat(highestEntryId, ":", highestSequence);
            logger.Info($"Publishing message {highestValue} to channel {SequencedStreamChannel}");

            var committed = await rcm.StreamAddListLeftPushStreamDeletePublishInTransactionAsync(sequencedStreamKey, sequencedListKey, lst, processedStreamKey, rvs, SequencedStreamChannel, highestValue);
            if (committed)
            {
                LastProcessedSequenceId = highestSequence;
                LastProcessedEntryId = highestEntryId;
                logger.Info($"Transaction handling {allEntryIds} successfully committed with LastProcessedEntryId={LastProcessedEntryId} and LastProcessedSequenceId={LastProcessedSequenceId}");
            }
            else
            {
                logger.Warn($"Transaction handling {allEntryIds} failed to commit keeping LastProcessedEntryId={LastProcessedEntryId} and LastProcessedSequenceId={LastProcessedSequenceId}");
            }
            
            return new Tuple<bool, string, long>(committed, highestEntryId, highestSequence);
        }
    }
}