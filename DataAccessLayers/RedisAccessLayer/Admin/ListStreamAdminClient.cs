using CommonTypes;

using log4net;
using StackExchange.Redis;

namespace RedisAccessLayer
{
    public class ListStreamAdminClient : ListStreamAdminBase, IListStreamAdminClient
    {
        private readonly RedisChannel PendingNewMessagesChannel;
        private static readonly ILog logger = LogManager.GetLogger(typeof(ListStreamAdminClient));

        public ListStreamAdminClient(IRedisConnectionManager cm)
            : base(cm)
        {
            var channelPrefix = EnvVarReader.GetString("REDIS_CHANNEL_PREFIX", EnvVarReader.NotFound);
            var prefix = channelPrefix != EnvVarReader.NotFound ? string.Concat("{", channelPrefix, "}:") : string.Empty;
            PendingNewMessagesChannel = RedisChannel.Literal(prefix + "NewMessages");
        }

        public async Task<List<MyMessage>> GetFullPendingList()
        {
            return await GetFullList(pendingListKey);
        }

        public async Task<List<MyMessage>> GetFullProcessingList()
        {
            return await GetFullList(processingListKey);
        }

        public async Task<List<MyMessage>> GetFullSequencedList()
        {
            return await GetFullList(sequencedListKey);
        }

        private async Task<List<MyMessage>> GetFullList(string listKey)
        {
            logger.Info($"Getting full list {listKey}");
            var listValues = await rcm.ListRangeAsync(listKey);
            #pragma warning disable CS8619
            return listValues.Select(v => v.ToString().FromShortString()).Where(mm => mm != null).ToList();
            #pragma warning restore CS8619
        }

        public async Task<List<MyStreamMessage>> GetFullSequencedStream()
        {
            return await GetAllMessagesFromStream(sequencedStreamKey);
        }

        public async Task<List<MyStreamMessage>> GetFullProcessedStream()
        {
            return await GetAllMessagesFromStream(processedStreamKey);
        }

        private async Task<List<MyStreamMessage>> GetAllMessagesFromStream(string streamKey)
        {
            var lst = new List<MyStreamMessage>();
            var lastEntryId = StreamPosition.Beginning;

            var entries = await rcm.StreamReadAsync(streamKey, lastEntryId);
            while (entries != null && entries.Length > 0)
            {
                foreach (StreamEntry entry in entries)
                {
                    var entryId = entry.Id.ToString();
                    var mm = entry.ToMyMessage();
                    lst.Add(mm);

                    lastEntryId = entry.Id;
                }
                entries = await rcm.StreamReadAsync(streamKey, lastEntryId);
            }
            return lst;
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
