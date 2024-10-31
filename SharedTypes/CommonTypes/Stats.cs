namespace CommonTypes
{
    public class Stats
    {
        private const int DecimalPrecision = 2;
        public Dictionary<string, double> CreatedToProcessingStats { get; private set; } = new();
        public Dictionary<string, double> ProcessingToProcessedStats { get; private set; } = new();
        public Dictionary<string, double> ProcessedToSequencingStats { get; private set; } = new();
        public Dictionary<string, double> SequencingToSavedStats { get; private set; } = new();
        public Dictionary<string, double> SavedToSequencedStats { get; private set; } = new();
        public Dictionary<string, double> ProcessingToSequencedStats { get; private set; } = new();
        public Dictionary<string, double> CreatedToSequencedStats { get; private set; } = new();

        public Dictionary<string, long> MaxCreatedToProcessingSeq { get; private set; } = new();
        public Dictionary<string, long> MaxProcessingToProcessedSeq { get; private set; } = new();
        public Dictionary<string, long> MaxProcessedToSequencingSeq { get; private set; } = new();
        public Dictionary<string, long> MaxSequencingToSavedSeq { get; private set; } = new();
        public Dictionary<string, long> MaxSavedToSequencedSeq { get; private set; } = new();
        public Dictionary<string, long> MaxProcessingToSequencedSeq { get; private set; } = new();
        public Dictionary<string, long> MaxCreatedToSequencedSeq { get; private set; } = new();

        public Dictionary<string, long> MinCreatedToProcessingSeq { get; private set; } = new();
        public Dictionary<string, long> MinProcessingToProcessedSeq { get; private set; } = new();
        public Dictionary<string, long> MinProcessedToSequencingSeq { get; private set; } = new();
        public Dictionary<string, long> MinSequencingToSavedSeq { get; private set; } = new();
        public Dictionary<string, long> MinSavedToSequencedSeq { get; private set; } = new();
        public Dictionary<string, long> MinProcessingToSequencedSeq { get; private set; } = new();
        public Dictionary<string, long> MinCreatedToSequencedSeq { get; private set; } = new();

        private List<MyMessageStats> stats = new();

       private Stats() { }

        public static async Task<Stats> CreateAsync(IEnumerable<MyMessage> mms)
        {
            var instance = new Stats();
            await instance.InitializeAsync(mms);
            return instance;
        }

        private async Task InitializeAsync(IEnumerable<MyMessage> mms)
        {
            // Convert messages to stats asynchronously
            stats.AddRange(await Task.Run(() => mms.Select(mm => new MyMessageStats(mm))));

            var orderTasks = new List<Task<IOrderedEnumerable<long>>>();
            orderTasks.Add(Task.Run(() => stats.Select(e => e.CreatedToProcessingTime).Order()));
            orderTasks.Add(Task.Run(() => stats.Select(e => e.ProcessingToProcessedTime).Order()));
            orderTasks.Add(Task.Run(() => stats.Select(e => e.ProcessedToSequencingTime).Order()));
            orderTasks.Add(Task.Run(() => stats.Select(e => e.SequencingToSavedTime).Order()));
            orderTasks.Add(Task.Run(() => stats.Select(e => e.SavedToSequencedTime).Order()));
            orderTasks.Add(Task.Run(() => stats.Select(e => e.ProcessingToSequencedTime).Order()));
            orderTasks.Add(Task.Run(() => stats.Select(e => e.CreatedToSequencedTime).Order()));
            // Wait for all ordering tasks to complete
            var results = await Task.WhenAll(orderTasks);
            var createdToProcessingList = results[0];
            var processingToProcessedList = results[1];
            var processedToSequencingList = results[2];
            var sequencingToSavedList = results[3];
            var savedToSequencedList = results[4];
            var processingToSequencedList = results[5];
            var createdToSequencedList = results[6];

            // Create all tasks for parallel processing
            var statsTasks = new List<Task>();
            // GetStats
            statsTasks.Add(Task.Run(() => CreatedToProcessingStats = GetStats(createdToProcessingList)));
            statsTasks.Add(Task.Run(() => ProcessingToProcessedStats = GetStats(processingToProcessedList)));
            statsTasks.Add(Task.Run(() => ProcessedToSequencingStats = GetStats(processedToSequencingList)));
            statsTasks.Add(Task.Run(() => SequencingToSavedStats = GetStats(sequencingToSavedList)));
            statsTasks.Add(Task.Run(() => SavedToSequencedStats = GetStats(savedToSequencedList)));
            statsTasks.Add(Task.Run(() => ProcessingToSequencedStats = GetStats(processingToSequencedList)));
            statsTasks.Add(Task.Run(() => CreatedToSequencedStats = GetStats(createdToSequencedList)));
            // GetSequenceAndMax
            statsTasks.Add(Task.Run(() => MaxCreatedToProcessingSeq = GetSequenceAndMax(e => e.CreatedToProcessingTime, createdToProcessingList.Last())));
            statsTasks.Add(Task.Run(() => MaxProcessingToProcessedSeq = GetSequenceAndMax(e => e.ProcessingToProcessedTime, processingToProcessedList.Last())));
            statsTasks.Add(Task.Run(() => MaxProcessedToSequencingSeq = GetSequenceAndMax(e => e.ProcessedToSequencingTime, processedToSequencingList.Last())));
            statsTasks.Add(Task.Run(() => MaxSequencingToSavedSeq = GetSequenceAndMax(e => e.SequencingToSavedTime, sequencingToSavedList.Last())));
            statsTasks.Add(Task.Run(() => MaxSavedToSequencedSeq = GetSequenceAndMax(e => e.SavedToSequencedTime, savedToSequencedList.Last())));
            statsTasks.Add(Task.Run(() => MaxProcessingToSequencedSeq = GetSequenceAndMax(e => e.ProcessingToSequencedTime, processingToSequencedList.Last())));
            statsTasks.Add(Task.Run(() => MaxCreatedToSequencedSeq = GetSequenceAndMax(e => e.CreatedToSequencedTime, createdToSequencedList.Last())));
            // GetSequenceAndMin
            statsTasks.Add(Task.Run(() => MinCreatedToProcessingSeq = GetSequenceAndMin(e => e.CreatedToProcessingTime, createdToProcessingList.First())));
            statsTasks.Add(Task.Run(() => MinProcessingToProcessedSeq = GetSequenceAndMin(e => e.ProcessingToProcessedTime, processingToProcessedList.First())));
            statsTasks.Add(Task.Run(() => MinProcessedToSequencingSeq = GetSequenceAndMin(e => e.ProcessedToSequencingTime, processedToSequencingList.First())));
            statsTasks.Add(Task.Run(() => MinSequencingToSavedSeq = GetSequenceAndMin(e => e.SequencingToSavedTime, sequencingToSavedList.First())));
            statsTasks.Add(Task.Run(() => MinSavedToSequencedSeq = GetSequenceAndMin(e => e.SavedToSequencedTime, savedToSequencedList.First())));
            statsTasks.Add(Task.Run(() => MinProcessingToSequencedSeq = GetSequenceAndMin(e => e.ProcessingToSequencedTime, processingToSequencedList.First())));
            statsTasks.Add(Task.Run(() => MinCreatedToSequencedSeq = GetSequenceAndMin(e => e.CreatedToSequencedTime, createdToSequencedList.First())));
            // Wait for all stats tasks to complete
            await Task.WhenAll(statsTasks);
        }

        // Keep this synchronous as it's called within Task.Run blocks
        public static Dictionary<string, double> GetStats(IOrderedEnumerable<long> values)
        {
            var dic = new Dictionary<string, double>
            {
                { "50p", Math.Round(CalculatePercentile(values, 0.50), DecimalPrecision) },
                { "90p", Math.Round(CalculatePercentile(values, 0.90), DecimalPrecision) },
                { "95p", Math.Round(CalculatePercentile(values, 0.95), DecimalPrecision) },
                { "99p", Math.Round(CalculatePercentile(values, 0.99), DecimalPrecision) },
                { "avg", Math.Round((double)values.Average(), DecimalPrecision) },
                { "min", Math.Round((double)values.First(), DecimalPrecision) },
                { "max", Math.Round((double)values.Last(), DecimalPrecision) }
            };
            return dic;
        }

        // Keep this synchronous as it's called within Task.Run blocks
        public static double CalculatePercentile(IOrderedEnumerable<long> values, double percentile)
        {
            if (values == null || !values.Any())
                throw new ArgumentException("The input array must not be null or empty", nameof(values));

            // Calculate the index corresponding to the desired percentile
            var realIndex = percentile * (values.Count() - 1);
            int index = (int)realIndex;
            var fraction = realIndex - index;

            return index + 1 < values.Count()
                ? values.ElementAt(index) * (1 - fraction) + values.ElementAt(index + 1) * fraction 
                : values.ElementAt(index);
        }

        public Dictionary<string, long> GetSequenceAndMax(Func<MyMessageStats, long> propertySelector, long maxTime)
        {
            if (stats == null || !stats.Any() || propertySelector == null)
            {
                return [];
            }

            var seq = stats.First(stat => propertySelector(stat) == maxTime).Sequence;
            var dic = new Dictionary<string, long>
            {
                { "max", maxTime },
                { "seq", seq }
            };
            return dic;
        }

        public Dictionary<string, long> GetSequenceAndMin(Func<MyMessageStats, long> propertySelector, long minTime)
        {
            if (stats == null || !stats.Any() || propertySelector == null)
            {
                return [];
            }

            var seq = stats.First(stat => propertySelector(stat) == minTime).Sequence;
            var dic = new Dictionary<string, long>
            {
                { "min", minTime },
                { "seq", seq }
            };
            return dic;
        }
    }
}