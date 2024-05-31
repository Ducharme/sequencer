using log4net;

namespace RedisAccessLayer
{
    public abstract class PendingListToProcessedStreamClientBase: ClientBase
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(PendingListToProcessedStreamClientBase));

        protected readonly string pendingListKey; // #1 Input
        protected readonly string processingListKey; // #2.0
        protected readonly string processedStreamKey; // #3.0

        protected readonly string sequencedStreamKey; // #4.0
        protected readonly string sequencedListKey; // #5 Output

        public string PendingListKey => this.pendingListKey;
        public string ProcessingListKey => this.processingListKey;
        public string ProcessedStreamKey => this.processedStreamKey;

        public PendingListToProcessedStreamClientBase(IRedisConnectionManager cm)
            : base (cm)
        {
            this.pendingListKey = AppendName(ListStreamKeyStrings.PendingListKey);
            this.processingListKey = AppendName(ListStreamKeyStrings.ProcessingListKey);
            this.processedStreamKey = AppendName(ListStreamKeyStrings.ProcessedStreamKey);

            this.sequencedStreamKey = AppendName(ListStreamKeyStrings.SequencedStreamKey);
            this.sequencedListKey = AppendName(ListStreamKeyStrings.SequencedListKey);

            logger.Info($"PendingListKey={pendingListKey}, ProcessingListKey={processingListKey}, ProcessedStreamKey={processedStreamKey}, SequencedStreamKey={sequencedStreamKey}, SequencedListKey={sequencedListKey}");
        }
    }
}