namespace RedisAccessLayer
{
    public abstract class ProcessedStreamToSequencedListClientBase: ClientBase
    {
        protected readonly string processedStreamKey; // #3.0
        protected readonly string sequencedStreamKey; // #4.0
        protected readonly string sequencedListKey; // #5 Output

        public string ProcessedStreamKey => this.processedStreamKey;
        public string SequencedStreamKey => this.sequencedStreamKey;
        public string SequencedListKey => this.sequencedListKey;

        public ProcessedStreamToSequencedListClientBase(IRedisConnectionManager cm)
            : base (cm)
        {
            this.processedStreamKey = AppendName(ListStreamKeyStrings.ProcessedStreamKey);
            this.sequencedStreamKey = AppendName(ListStreamKeyStrings.SequencedStreamKey);
            this.sequencedListKey = AppendName(ListStreamKeyStrings.SequencedListKey);
        }
    }
}