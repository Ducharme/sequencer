namespace CommonTypes
{
    public class MyMessageStats(MyMessage mm)
    {
        public long Sequence { get; private set; } = mm.Sequence;
        public long CreatedToProcessingTime { get; private set; } = mm.ProcessingAt - mm.CreatedAt;
        public long ProcessingToProcessedTime { get; private set; } = mm.ProcessedAt - mm.ProcessingAt;
        public long ProcessedToSequencingTime { get; private set; } = mm.SequencingAt - mm.ProcessedAt;
        public long SequencingToSavedTime { get; private set; } = mm.SavedAt - mm.SequencingAt;
        public long SavedToSequencedTime { get; private set; } = mm.SequencedAt - mm.SavedAt;

        public long ProcessingToSequencedTime { get; private set; } = mm.SequencedAt - mm.ProcessingAt;
        public long CreatedToSequencedTime { get; private set; } = mm.SequencedAt - mm.CreatedAt;
    }
}