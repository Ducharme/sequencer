namespace RedisAccessLayer
{
    public interface IPendingToProcessedBase
    {
        string PendingListKey { get; }
        string ProcessingListKey { get; }
        string? LastProcessedEntryId { get; }
        long? LastProcessedSequenceId { get; }
    }
}
