namespace RedisAccessLayer
{
    public abstract class ListStreamAdminBase: ClientBase
    {
        protected readonly string pendingListKey; // #1 Input
        protected readonly string processingListKey; // #2.0
        protected readonly string processedStreamKey; // #3.0
        protected readonly string sequencedStreamKey; // #4.0
        protected readonly string sequencedListKey; // #5 Output

        public string PendingListKey => this.pendingListKey;
        public string ProcessingListKey => this.processingListKey;
        public string ProcessedStreamKey => this.processedStreamKey;
        public string SequencedStreamKey => this.sequencedStreamKey;
        public string SequencedListKey => this.sequencedListKey;

        public ListStreamAdminBase(IRedisConnectionManager cm)
            : base (cm)
        {
            this.pendingListKey = AppendName(ListStreamKeyStrings.PendingListKey);
            this.processingListKey = AppendName(ListStreamKeyStrings.ProcessingListKey);
            this.processedStreamKey = AppendName(ListStreamKeyStrings.ProcessedStreamKey);
            this.sequencedStreamKey = AppendName(ListStreamKeyStrings.SequencedStreamKey);
            this.sequencedListKey = AppendName(ListStreamKeyStrings.SequencedListKey);
        }
    }
}