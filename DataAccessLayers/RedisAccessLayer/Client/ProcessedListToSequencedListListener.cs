using log4net;
using StackExchange.Redis;

using CommonTypes;

namespace RedisAccessLayer
{
    public class ProcessedListToSequencedListListener : ProcessedStreamToSequencedListClientBase, IProcessedToSequencedListener
    {
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

        public async Task ListenForPendingMessages(Func<List<MyStreamMessage>, Task<bool>> handler)
        {
            rcm.AddSubscription(ProcessedStreamChannel, SubscribeToProcessedChannelHandler);
            rcm.AddSubscription(SequencedStreamChannel, SubscribeToSequencedChannelHandler);

            var subscribed = false;
            var listen = Interlocked.Read(ref this.shouldListen);
            var exiting = Interlocked.Read(ref this.isExiting);
            while (listen == 1 && exiting == 0 && !subscribed)
            {
                try
                {
                    await rcm.SubscribeAsync();
                    subscribed = true;
                }
                catch (RedisTimeoutException)
                {
                    listen = Interlocked.Read(ref this.shouldListen);
                    exiting = Interlocked.Read(ref this.isExiting);
                    if (listen == 1 && exiting == 0)
                    {
                        // https://stackexchange.github.io/StackExchange.Redis/Timeouts
                        logger.Warn("Redis client encountered a timeout while subscribing, waiting to reconnect");
                        await Task.Delay(LongWaitTime);
                        await rcm.Reconnect();
                    }
                    else
                    {
                        logger.Warn("Redis client encountered a timeout while subscribing, pending to exit");
                    }
                }
                catch (Exception ex) when (ex is RedisServerException || ex is RedisTimeoutException || ex is RedisConnectionException || ex is RedisException)
                {
                    listen = Interlocked.Read(ref this.shouldListen);
                    exiting = Interlocked.Read(ref this.isExiting);
                    if (listen == 1 && exiting == 0)
                    {
                        logger.Warn("Redis client encountered an error while subscribing, waiting to reconnect");
                        await Task.Delay(LongWaitTime);
                    }
                    else
                    {
                        logger.Warn("Redis client encountered an error while subscribing, pending to exit");
                    }
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

                        var count = Interlocked.Read(ref pendingMessages);
                        logger.Debug($"pendingMessages={count} && sequencingStatus={sequencingStatus}");
                        if (count == 0 && sequencingStatus == SequencingStatus.WasEmpty)
                        {
                            newMessageEvent.Wait(WaitTime);
                        }

                        newMessageEvent.Reset();
                        var entries = await rcm.StreamReadAsync(processedStreamKey, StreamPosition.Beginning);
                        logger.Debug($"StreamReadAsync entries={entries.Length}");
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
                    listen = Interlocked.Read(ref this.shouldListen);
                }
                catch (RedisTimeoutException)
                {
                    listen = Interlocked.Read(ref this.shouldListen);
                    if (listen == 1)
                    {
                        // https://stackexchange.github.io/StackExchange.Redis/Timeouts
                        logger.Warn("Redis client encountered a timeout while listening, waiting to reconnect");
                        await Task.Delay(LongWaitTime);
                        await rcm.Reconnect();
                    }
                    else
                    {
                        logger.Warn("Redis client encountered a timeout while listening, pending to exit");
                    }
                }
                catch (Exception ex) when (ex is RedisServerException || ex is RedisTimeoutException || ex is RedisConnectionException || ex is RedisException) // Error with RedisCommandException should exit
                {
                    listen = Interlocked.Read(ref this.shouldListen);
                    if (listen == 1)
                    {
                        logger.Warn("Redis client encountered an error while listening, waiting to retry");
                        await Task.Delay(LongWaitTime);
                    }
                    else
                    {
                        logger.Warn("Redis client encountered an error while listening, pending to exit");
                    }
                }
            }
            Interlocked.Exchange(ref this.isExiting, 1);
        }

        public static List<long> FindMissingSequenceIds(List<long> incomplete)
        {
            // Generate a range of long from min to max (inclusive)
            var fullRange = GenerateLongRange(incomplete);
            // Find the long in the range that are not in the incomplete list
            return fullRange.Except(incomplete).ToList();
        }

        private static IEnumerable<long> GenerateLongRange(List<long> incomplete)
        {
            long start = incomplete.Min();
            long end = incomplete.Max();
            for (long i = start; i <= end; i++)
            {
                yield return i;
            }
        }

        private async Task<string[]> SequenceEntries(Func<List<MyStreamMessage>, Task<bool>> handler, string[] lastEntries, StreamEntry[] entries)
        {
            var thisEntries = entries.Select(e => e.Id.ToString()).ToArray();
            if (!thisEntries.SequenceEqual(lastEntries))
            {
                logger.Info($"Received new stream ids {string.Join(",", thisEntries)}");
                lastEntries = thisEntries;

                var lst = entries.Select(e => e.ToMyMessage()).OrderBy(msm => msm.Sequence).ToList();
                long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                foreach (var msm in lst)
                {
                    msm.SequencingAt = timestamp;
                }

                var ids = lst.Select(kvp => kvp.Sequence).Order().ToList(); // TODO: Remove Order()
                var sequence = SequenceHelper.GetSequence(LastProcessedSequenceId, ids);
                if (logger.IsDebugEnabled)
                {
                    if (!sequence.IsComplete)
                    {
                        var missingIds = FindMissingSequenceIds(ids);
                        var missingIdsJoined = string.Join(",", missingIds.Order());
                        logger.Debug($"Sequence range is from {sequence.Min} to {sequence.Max} but ordered up to {sequence.LastInOrder}, expecting {sequence.ExpectedNext} but next is {sequence.ActualNext}. LastProcessedSequenceId={sequence.LastProcessed}, IsSequenceComplete={sequence.IsComplete}, PartialSequence.Count={sequence.List.Count}, MissingSequenceIds={missingIdsJoined}");
                    }
                    else
                    {
                        logger.Debug($"Sequence range is from {sequence.Min} to {sequence.Max}. IsSequenceComplete: {sequence.IsComplete} & Count {sequence.List.Count}");
                    }
                }

                List<MyStreamMessage> filtered;
                if (sequence.IsComplete)
                {
                    filtered = lst;
                }
                else if (sequence.List.Count > 0)
                {
                    filtered = lst.Where(msm => sequence.List.Contains(msm.Sequence)).OrderBy(msm => msm.Sequence).ToList(); // TODO: Remove Order()
                }
                else
                {
                    filtered = [];
                }

                if (filtered.Count > 0)
                {
                    // TODO: When dic has more than 1000 entries, call syncLock.ExtendLock(defaultLockTime) and process by batch
                    var success = await handler(filtered);
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
                    logger.Info($"Received message {message} from channel {channel} with LastProcessedSequenceId={LastProcessedSequenceId}");
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
            // Use StreamPosition.NewMessages "$" for last message and StreamPosition.Beginning "-" for all messages
            string? lastKey = StreamPosition.Beginning;
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
                        logger.Info($"Received stream info for {sequencedStreamKey} with LastProcessedSequenceId={LastProcessedSequenceId}");
                    }
                    else
                    {
                        logger.Warn($"Received stream info for {sequencedStreamKey} with missing information to set LastProcessedEntryId and LastProcessedSequenceId");
                    }
                }
                else
                {
                    logger.Info($"Stream {sequencedStreamKey} does not exist, LastProcessedSequenceId={LastProcessedSequenceId}");
                }
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

        private struct Batch
        {
            public Dictionary<string, MyMessage> Dic = [];

            public Batch() { }
        }

        public async Task<Tuple<bool, string, long>> FromProcessedToSequenced(List<MyStreamMessage> msms)
        {
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var lst = new List<Tuple<string, NameValueEntry[]>>();
            foreach (var msm in msms)
            {
                msm.SequencedAt = timestamp;
                var nves = msm.ToNameValueEntries();
                var str = msm.ToShortString() ?? string.Empty;
                logger.Info($"Appending sequencing message streamEntryId {msm.StreamId} with sequence id {msm.Sequence} to {sequencedStreamKey} and {sequencedListKey}");
                lst.Add(new Tuple<string, NameValueEntry[]>(str, nves));
            }

            var streamDeleteValues = msms.Where(msm => !string.IsNullOrEmpty(msm.StreamId)).Select(msm => new RedisValue(msm.StreamId!)).ToArray();
            logger.Info($"Deleting these streamIds from {processedStreamKey}: {string.Join(",", streamDeleteValues)}");

            var highestSequence = msms.Max(msm => msm.Sequence);
            var highestValue = string.Concat("NA:", highestSequence);
            logger.Info($"Publishing message {highestValue} to channel {SequencedStreamChannel}");

            var committed = await rcm.StreamAddListLeftPushStreamDeletePublishInTransactionAsync(sequencedStreamKey, sequencedListKey, lst, processedStreamKey, streamDeleteValues, SequencedStreamChannel, highestValue);
            var first = msms.First();
            var last = msms.Last();
            if (committed)
            {
                LastProcessedSequenceId = highestSequence;
                logger.Info($"Transaction handling {msms.Count} entries from {first.StreamId}({first.Sequence}) to {last.StreamId}({last.Sequence}) successfully committed with LastProcessedSequenceId={LastProcessedSequenceId}");
            }
            else
            {
                logger.Warn($"Transaction handling {msms.Count} entries from {first.StreamId}({first.Sequence}) to {last.StreamId}({last.Sequence}) failed to commit keeping LastProcessedSequenceId={LastProcessedSequenceId}");
            }
            
            return new Tuple<bool, string, long>(committed, "NA", highestSequence);
        }
    }
}