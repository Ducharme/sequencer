namespace RedisAccessLayer
{
    public interface IProcessedToSequencedBase
    {
        string ProcessedStreamKey { get; }
        string SequencedStreamKey { get; }
        string SequencedListKey { get; }
        long? LastProcessedSequenceId { get; }
    }
}
