namespace RedisAccessLayer
{
    internal static class KeyStrings
    {
        public static readonly string PendingListKey = "pending-lst"; // #1 Input
        public static readonly string ProcessingListKey = "processing-lst"; // #2.0
        public static readonly string ProcessedStreamKey = "processed-str"; // #3.0
        public static readonly string SequencedStreamKey = "sequenced-str"; // #4.0
        public static readonly string SequencedListKey = "sequenced-lst"; // #5 Output
    }

    internal static class ListStreamKeyStrings
    {
        public static readonly string PendingListKey = KeyStrings.PendingListKey;
        public static readonly string ProcessingListKey = KeyStrings.ProcessingListKey;
        public static readonly string ProcessedStreamKey = KeyStrings.ProcessedStreamKey;
        public static readonly string SequencedStreamKey = KeyStrings.SequencedStreamKey;
        public static readonly string SequencedListKey = KeyStrings.SequencedListKey;
    }
}