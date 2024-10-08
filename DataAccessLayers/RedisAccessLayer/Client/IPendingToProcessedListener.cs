using CommonTypes;

namespace RedisAccessLayer
{
    public interface IPendingToProcessedListener : IPendingToProcessedBase
    {
        Task ListenMessagesFromPendingList(Func<string, string, MyMessage, Task<bool>> handler);
        Task<bool> AppendToProcessedStreamThenDeleteFromProcessingList(MyMessage mm, string str);
        Task<bool> CanMessageBeProcessed(string name, long sequence);
    }
}