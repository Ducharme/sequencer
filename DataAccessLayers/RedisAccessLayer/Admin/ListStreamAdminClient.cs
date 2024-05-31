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

       public async Task<List<MyMessage>> GetFullPendingStream()
        {
            return await GetAllMessagesFromStream(processedStreamKey);
        }

        public async Task<List<MyMessage>> GetFullProcessedStream()
        {
            return await GetAllMessagesFromStream(sequencedStreamKey);
        }

        private async Task<List<MyMessage>> GetAllMessagesFromStream(string streamKey)
        {
            var lst = new List<MyMessage>();
            var lastEntryId = "-";

            var entries = await rcm.StreamReadAsync(streamKey, lastEntryId);
            while (entries != null && entries.Length > 0)
            {
                foreach (StreamEntry entry in entries)
                {
                    var entryId = entry.Id.ToString();
                    MyMessage mm = entry.ToMyMessage();
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
            string? lastKey = "$"; // Use "$" for last message and "-" for all messages
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
    }
}
