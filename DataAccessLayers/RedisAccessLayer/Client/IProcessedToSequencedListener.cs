using CommonTypes;

namespace RedisAccessLayer
{
    public interface IProcessedToSequencedListener : IProcessedToSequencedBase
    {
        Task ListenForPendingMessages(Func<Dictionary<string, MyStreamMessage>, Task<bool>> handler);
        Task SetLastMessageFromSequencedStream();
        Task<KeyValuePair<string, MyMessage>> GetLastMessageFromPendingStream();
        Task<Tuple<bool, string, long>> FromProcessedToSequenced(Dictionary<string, MyStreamMessage> dic);
    }
}
