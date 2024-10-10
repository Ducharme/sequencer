using CommonTypes;

namespace RedisAccessLayer
{
    public interface IProcessedToSequencedListener : IProcessedToSequencedBase
    {
        Task ListenForPendingMessages(Func<List<MyStreamMessage>, Task<bool>> handler);
        Task SetLastMessageFromSequencedStream();
        Task<KeyValuePair<string, MyMessage>> GetLastMessageFromPendingStream();
        Task<Tuple<bool, string, long>> FromProcessedToSequenced(List<MyStreamMessage> lst);
    }
}
