using System.Threading.Tasks;
using CommonTypes;

namespace AdminService
{
    public interface IAdminManager
    {
        Task<int> PrepareDatabase(string name);
        Task<bool> DeleteStreams(string name);
        Task<bool> GeneratePendingList(string name, int numberOfMessages, int creationDelay, int processingTime);
        Task<bool> DeleteLists(string name);
        Task<List<MyMessage>> GetAllMessagesFromDatabase(string name);
        Task<List<MyMessage>> GetAllMessagesFromPendingList(string name);
        Task<List<MyMessage>> GetAllMessagesFromProcessingList(string name);
        Task<List<MyMessage>> GetAllMessagesFromSequencedList(string name);
        void ClearCache();

        Task<List<MyStreamMessage>> GetAllMessagesFromSequencedStream(string name);
        Task<List<MyStreamMessage>> GetAllMessagesFromProcessedStream(string name);
        Task<long> GetSequencedListMessagesCount(string name);
        Task<long> GetSequencedStreamMessagesCount(string name);
        Task<MyMessage?> GetSequencedStreamLastMessage(string name);

        Task<string> RedisServerInfos();
    }
}