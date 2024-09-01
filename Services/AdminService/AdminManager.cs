using System.Threading.Tasks;

using RedisAccessLayer;
using DatabaseAccessLayer;
using CommonTypes;

using log4net;

namespace AdminService
{
    public class AdminManager: IAdminManager
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(AdminManager));

        private IDatabaseAdmin database_admin;
        private IListStreamAdminClient list_stream_manager;

        public AdminManager(IDatabaseAdmin dba, IListStreamAdminClient slm)
        {
            database_admin = dba ?? throw new NullReferenceException("IDatabaseAdmin implementation could not be resolved");
            list_stream_manager = slm ?? throw new NullReferenceException("IListStreamManager implementation could not be resolved");
        }

        #pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task<int> PrepareDatabase(string name)
        {
            database_admin.DropTable();
            database_admin.CreateTable();
            return database_admin.PurgeTable();
        }
        #pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

        public async Task<bool> DeleteStreams(string name)
        {
            var dspe = await list_stream_manager.DeletePendingStream();
            var dspr = await list_stream_manager.DeleteProcessedStream();
            return dspe.HasValue && dspe.Value && dspr.HasValue && dspr.Value;
        }

        public async Task<bool> GeneratePendingList(string name, int numberOfMessages, int creationDelay, int processingTime)
        {
            const char firstLetter = 'a';
            var mms = new List<MyMessage>();
            for (var i=0; i < numberOfMessages; i++)
            {
                var alphabetIndex = i % 26;
                var mm = new MyMessage
                {
                    Sequence = i + 1,
                    Name = name,
                    CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    Payload = ((char)(firstLetter + alphabetIndex)).ToString(),
                    Delay = processingTime
                };
                mms.Add(mm);
            }

            if (creationDelay == 0)
            {
                return await list_stream_manager.PushToPendingList(mms) ?? false;
            }
            else
            {
                _ = Task.Run(async () =>
                {
                    foreach (var mm in mms)
                    {
                        await Task.Delay(creationDelay);
                        mm.CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        _ = list_stream_manager.PushToPendingList(mm);
                    }
                });
                return true;
            }
        }

        public async Task<bool> DeleteLists(string name) //TODO: Use or remove name
        {
            var t1 = await list_stream_manager.DeleteProcessingList();
            var t2 = await list_stream_manager.DeletePendingList();
            var t3 = await list_stream_manager.DeleteProcessedList();
            return t1.HasValue && t1.Value && t2.HasValue && t2.Value && t3.HasValue && t3.Value;
        }

        #pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task<List<MyMessage>> GetAllMessagesFromDatabase(string name)
        {
            return database_admin.GetAllMessages(name);
        }
        #pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

        public async Task<List<MyMessage>> GetAllMessagesFromPendingList(string name)
        {
            return await list_stream_manager.GetFullPendingList();
        }

        public async Task<List<MyMessage>> GetAllMessagesFromProcessingList(string name)
        {
            return await list_stream_manager.GetFullProcessingList();
        }

        public async Task<List<MyMessage>> GetAllMessagesFromSequencedList(string name)
        {
            return await list_stream_manager.GetFullSequencedList();
        }

        public async Task<List<MyMessage>> GetAllMessagesFromPendingStream(string name)
        {
            return await list_stream_manager.GetFullPendingStream();
        }

        public async Task<List<MyMessage>> GetAllMessagesFromProcessedStream(string name)
        {
            return await list_stream_manager.GetFullProcessedStream();
        }

        public async Task<string> RedisServerInfos()
        {
            return await list_stream_manager.RedisServerInfos();
        }
    }
}