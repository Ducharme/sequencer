using CommonTypes;

namespace RedisAccessLayer
{
    public interface IListStreamAdminClient
    {
        Task<List<MyMessage>> GetFullPendingList();
        Task<List<MyMessage>> GetFullProcessingList();
        Task<List<MyMessage>> GetFullSequencedList();
        Task<List<MyStreamMessage>> GetFullSequencedStream();
        Task<List<MyStreamMessage>> GetFullProcessedStream();
        Task PrintLastValuesFromProcessedStream();

        Task<bool?> PushToPendingList(IEnumerable<MyMessage> mms);
        Task<bool?> PushToPendingList(MyMessage mm);

        Task<bool?> DeletePendingList();
        Task<bool?> DeleteProcessingList();
        Task<bool?> DeletePendingStream();
        Task<bool?> DeleteProcessedStream();
        Task<bool?> DeleteProcessedList();

        Task<long> GetSequencedListMessagesCount();
        Task<long> GetSequencedStreamMessagesCount();
        Task<MyMessage?> GetSequencedStreamLastMessage();

        Task<string> RedisServerInfos();
        Task<TimeSpan> Ping();
    }
}