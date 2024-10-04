namespace CommonTypes
{
    public class Stats
    {
        private const int DecimalPrecision = 2;
        public Dictionary<string, double> CreatedToProcessingStats { get; }
        public Dictionary<string, double> ProcessingToProcessedStats { get; }
        public Dictionary<string, double> ProcessedToSequencingStats { get; }
        public Dictionary<string, double> SequencingToSavedStats { get; }
        public Dictionary<string, double> SavedToSequencedStats { get; }
        public Dictionary<string, double> ProcessingToSequencedStats { get; }
        public Dictionary<string, double> CreatedToSequencedStats { get; }

        public Dictionary<string, long> MaxCreatedToProcessingSeq { get; }
        public Dictionary<string, long> MaxProcessingToProcessedSeq { get; }
        public Dictionary<string, long> MaxProcessedToSequencingSeq { get; }
        public Dictionary<string, long> MaxSequencingToSavedSeq { get; }
        public Dictionary<string, long> MaxSavedToSequencedSeq { get; }
        public Dictionary<string, long> MaxProcessingToSequencedSeq { get; }
        public Dictionary<string, long> MaxCreatedToSequencedSeq { get; }

        public Dictionary<string, long> MinCreatedToProcessingSeq { get; }
        public Dictionary<string, long> MinProcessingToProcessedSeq { get; }
        public Dictionary<string, long> MinProcessedToSequencingSeq { get; }
        public Dictionary<string, long> MinSequencingToSavedSeq { get; }
        public Dictionary<string, long> MinSavedToSequencedSeq { get; }
        public Dictionary<string, long> MinProcessingToSequencedSeq { get; }
        public Dictionary<string, long> MinCreatedToSequencedSeq { get; }

        private readonly List<MyMessageStats> stats;

        public Stats(IEnumerable<MyMessage> mms)
        {
            stats = mms.Select(mm => new MyMessageStats(mm)).ToList();
            CreatedToProcessingStats = GetStats(stats.Select(e => e.CreatedToProcessingTime).ToList());
            ProcessingToProcessedStats = GetStats(stats.Select(e => e.ProcessingToProcessedTime).ToList());
            ProcessedToSequencingStats = GetStats(stats.Select(e => e.ProcessedToSequencingTime).ToList());
            SequencingToSavedStats = GetStats(stats.Select(e => e.SequencingToSavedTime).ToList());
            SavedToSequencedStats = GetStats(stats.Select(e => e.SavedToSequencedTime).ToList());
            ProcessingToSequencedStats = GetStats(stats.Select(e => e.ProcessingToSequencedTime).ToList());
            CreatedToSequencedStats = GetStats(stats.Select(e => e.CreatedToSequencedTime).ToList());

            MaxCreatedToProcessingSeq = stats.GetSequenceAndMax(e => e.CreatedToProcessingTime);
            MaxProcessingToProcessedSeq = stats.GetSequenceAndMax(e => e.ProcessingToProcessedTime);
            MaxProcessedToSequencingSeq = stats.GetSequenceAndMax(e => e.ProcessedToSequencingTime);
            MaxSequencingToSavedSeq = stats.GetSequenceAndMax(e => e.SequencingToSavedTime);
            MaxSavedToSequencedSeq = stats.GetSequenceAndMax(e => e.SavedToSequencedTime);
            MaxProcessingToSequencedSeq = stats.GetSequenceAndMax(e => e.ProcessingToSequencedTime);
            MaxCreatedToSequencedSeq = stats.GetSequenceAndMax(e => e.CreatedToSequencedTime);

            MinCreatedToProcessingSeq = stats.GetSequenceAndMin(e => e.CreatedToProcessingTime);
            MinProcessingToProcessedSeq = stats.GetSequenceAndMin(e => e.ProcessingToProcessedTime);
            MinProcessedToSequencingSeq = stats.GetSequenceAndMin(e => e.ProcessedToSequencingTime);
            MinSequencingToSavedSeq = stats.GetSequenceAndMin(e => e.SequencingToSavedTime);
            MinSavedToSequencedSeq = stats.GetSequenceAndMin(e => e.SavedToSequencedTime);
            MinProcessingToSequencedSeq = stats.GetSequenceAndMin(e => e.ProcessingToSequencedTime);
            MinCreatedToSequencedSeq = stats.GetSequenceAndMin(e => e.CreatedToSequencedTime);
        }

        public static Dictionary<string, double> GetStats(List<long> values)
        {
            var dic = new Dictionary<string, double>
            {
                { "50p", Math.Round(CalculatePercentile(values, 0.50), DecimalPrecision) },
                { "90p", Math.Round(CalculatePercentile(values, 0.90), DecimalPrecision) },
                { "95p", Math.Round(CalculatePercentile(values, 0.95), DecimalPrecision) },
                { "99p", Math.Round(CalculatePercentile(values, 0.99), DecimalPrecision) },
                { "avg", Math.Round((double)values.Average(), DecimalPrecision) },
                { "min", Math.Round((double)values.Min(), DecimalPrecision) },
                { "max", Math.Round((double)values.Max(), DecimalPrecision) }
            };
            return dic;
        }

        public static double CalculatePercentile(List<long> values, double percentile)
        {
            if (values == null || values.Count == 0)
                throw new ArgumentException("The input array must not be empty", nameof(values));

            // Sort the array in ascending order
            values.Sort();

            // Calculate the index corresponding to the desired percentile
            var realIndex = percentile * (values.Count - 1);
            int index = (int)realIndex;
            var fraction = realIndex - index;

            // Interpolate the value at the percentile
            return index + 1 < values.Count ? values[index] * (1 - fraction) + values[index + 1] * fraction : values[index];
        }
    }
}