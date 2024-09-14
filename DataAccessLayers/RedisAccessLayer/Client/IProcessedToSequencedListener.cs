using CommonTypes;

namespace RedisAccessLayer
{
    public interface IProcessedToSequencedListener : IProcessedToSequencedBase
    {
         Task ListenForPendingMessages(Func<Dictionary<string, MyMessage>, Task<bool>> handler);
        Task SetLastMessageFromSequencedStream();
        Task<KeyValuePair<string, MyMessage>> GetLastMessageFromPendingStream();
        Task<Tuple<bool, string, long>> FromProcessedToSequenced(Dictionary<string, MyMessage> dic);
    }
}
