using CommonTypes;

using log4net;
using StackExchange.Redis;

namespace RedisAccessLayer
{
    public class ListStreamAdminClient : ListStreamAdminBase, IListStreamAdminClient
    {
        private readonly RedisChannel PendingNewMessagesChannel;
        private static readonly ILog logger = LogManager.GetLogger(typeof(ListStreamAdminClient));

        private List<MyMessage>? pendingListCache = null;
        private List<MyMessage>? processingListCache = null;
        private List<MyMessage>? sequencedListCache = null;
        private List<MyStreamMessage>? sequencedStreamCache = null;
        private List<MyStreamMessage>? processedStreamCache = null;
        private readonly object cachelLock = new();


        public ListStreamAdminClient(IRedisConnectionManager cm)
            : base(cm)
        {
            var channelPrefix = EnvVarReader.GetString("REDIS_CHANNEL_PREFIX", EnvVarReader.NotFound);
            var prefix = channelPrefix != EnvVarReader.NotFound ? string.Concat("{", channelPrefix, "}:") : string.Empty;
            PendingNewMessagesChannel = RedisChannel.Literal(prefix + "NewMessages");
        }

        public async Task<List<MyMessage>> GetFullPendingList()
        {
            lock (cachelLock)
            {
                if (pendingListCache != null)
                {
                    return pendingListCache;
                }
            }

            var lst = await GetFullList(pendingListKey);
            lock (cachelLock)
            {
                pendingListCache = lst;
            }
            return lst;
        }

        public async Task<List<MyMessage>> GetFullProcessingList()
        {
            lock (cachelLock)
            {
                if (processingListCache != null)
                {
                    return processingListCache;
                }
            }

            var lst = await GetFullList(processingListKey);
            lock (cachelLock)
            {
                processingListCache = lst;
            }
            return lst;
        }

        public async Task<List<MyMessage>> GetFullSequencedList()
        {
            lock (cachelLock)
            {
                if (sequencedListCache != null)
                {
                    return sequencedListCache;
                }
            }
            
            var lst = await GetFullList(sequencedListKey);
            lock (cachelLock)
            {
                sequencedListCache = lst;
            }
            return lst;
        }

        public void ClearCache()
        {
            lock (cachelLock)
            {
                pendingListCache = null;
                processingListCache = null;
                sequencedListCache = null;
                sequencedStreamCache = null;
                processedStreamCache = null;
            }
        }

        private async Task<List<MyMessage>> GetFullList(string listKey)
        {
            logger.Info($"Getting full list {listKey}");
            var listValues = await rcm.ListAllPipelinedAsync(listKey);
            //var listValues = await rcm.ListAllAsync(listKey);
            #pragma warning disable CS8619
            return listValues.Select(v => v.ToString().FromShortString()).Where(mm => mm != null).ToList();
            #pragma warning restore CS8619
        }

        public async Task<List<MyStreamMessage>> GetFullSequencedStream()
        {
            lock (cachelLock)
            {
                if (sequencedStreamCache != null)
                {
                    return sequencedStreamCache;
                }
            }
            
            var lst = await GetAllMessagesFromStream(sequencedStreamKey);
            lock (cachelLock)
            {
                sequencedStreamCache = lst;
            }
            return lst;
        }

        public async Task<List<MyStreamMessage>> GetFullProcessedStream()
        {
            lock (cachelLock)
            {
                if (processedStreamCache != null)
                {
                    return processedStreamCache;
                }
            }
            
            var lst = await GetAllMessagesFromStream(processedStreamKey);
            lock (cachelLock)
            {
                processedStreamCache = lst;
            }
            return lst;
        }

        private async Task<List<MyStreamMessage>> GetAllMessagesFromStream(string streamKey)
        {
            var entries = await rcm.StreamReadAllAsync(streamKey);
            return entries.Select(se => se.ToMyMessage()).ToList();
        }

        public async Task PrintLastValuesFromProcessedStream()
        {
            // Handle entryId whe starting afterward (should resume to the next key not the first)
            // Use StreamPosition.NewMessages "$" for last message and StreamPosition.Beginning "-" for all messages
            string? lastKey = StreamPosition.NewMessages;
            var entries = await rcm.StreamReadAsync(sequencedStreamKey, lastKey);
            logger.Debug($"Stream {sequencedStreamKey} received entries");
            if (entries != null)
            {
                if (entries.Length > 0)
                {
                    foreach (var entry in entries)
                    {
                        var lst = new List<string>();
                        for (var i=0; i < entry.Values.Length; i++)
                        {
                            var nve = entry.Values[i];
                            lst.Add(nve.Name + ":" + nve.Value);
                        }
                        var str = string.Join(", ", lst);
                        logger.Info($"Stream {sequencedStreamKey} with id {entry.Id} contains values {str}");
                    }
                } else {
                    logger.Info($"Stream {sequencedStreamKey} received 0 entry");
                }
            } else {
                logger.Info($"Stream {sequencedStreamKey} received null entry");
            }
        }

        public async Task<bool?> PushToPendingList(IEnumerable<MyMessage> mms)
        {
            if (mms == null || !mms.Any())
            {
                return false;
            }

            var count = mms.Count();
            var committed = await rcm.ListLeftPushPublishInTransactionAsync(pendingListKey, mms, PendingNewMessagesChannel, count);
            var first = mms.First().Sequence;
            var last = mms.Last().Sequence;
            if (committed)
            {
                logger.Info($"List {pendingListKey} pushed values from Sequence {first} to {last} in a transaction");
            }
            else
            {
                logger.Warn($"List {pendingListKey} failed to push values from Sequence {first} to {last} in a transaction");
            }
            return committed;
        }

        public async Task<bool?> PushToPendingList(MyMessage mm)
        {
            if (mm == null)
            {
                return false;
            }

            var str = mm.ToShortString() ?? string.Empty;
            logger.Debug($"Pushing message {mm} to list {pendingListKey}");
            var committed = await rcm.ListLeftPushPublishInTransactionAsync(pendingListKey, str, PendingNewMessagesChannel, 1);
            return committed;
        }

        public async Task<bool?> DeletePendingList()
        {
            return await DeleteList(pendingListKey);
        }

        public async Task<bool?> DeleteProcessingList()
        {
            return await DeleteList(processingListKey);
        }

        public async Task<bool?> DeletePendingStream()
        {
            return await DeleteStream(processedStreamKey);
        }

        public async Task<bool?> DeleteProcessedStream()
        {
            return await DeleteStream(sequencedStreamKey);
        }

        private async Task<bool?> DeleteStream(string streamKey)
        {
            logger.Info($"Deleting stream {streamKey}");
            return await rcm.KeyDeleteAsync(streamKey);
        }

        public async Task<bool?> DeleteProcessedList()
        {
            return await DeleteList(sequencedListKey);
        }

        private async Task<bool?> DeleteList(string listKey)
        {
            logger.Info($"Deleting list {listKey}");
            return await rcm.KeyDeleteAsync(listKey);
        }

        public async Task<long> GetSequencedListMessagesCount()
        {
            logger.Info($"Getting list messages count {sequencedListKey}");
            return await rcm.GetListMessagesCount(sequencedListKey);
        }

        public async Task<long> GetSequencedStreamMessagesCount()
        {
            logger.Info($"Getting stream messages count {sequencedStreamKey}");
            return await rcm.GetStreamMessagesCount(sequencedStreamKey);
        }

        public async Task<MyMessage?> GetSequencedStreamLastMessage()
        {
            logger.Info($"Getting stream last message count {sequencedStreamKey}");
            return await rcm.GetStreamLastMessage(sequencedStreamKey);
        }

        public async Task<string> RedisServerInfos()
        {
            logger.Info("Logging servers commands");
            return await rcm.ServerInfos();
        }

        public async Task<TimeSpan> Ping()
        {
            logger.Info("Ping");
            return await rcm.Ping();
        }
    }
}
