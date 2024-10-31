using System.Text.Json.Serialization;
using log4net;

namespace CommonTypes
{
    public class Perfs
    {
        [JsonIgnore]
        [NonSerialized]
        private static readonly ILog logger = LogManager.GetLogger(typeof(Perfs));

        private const int DecimalPrecision = 2;
        public Dictionary<long, long> ProcessingRatePerSecond { get; } = new();
        public Dictionary<long, long> SequencingRatePerSecond { get; } = new();
        public Dictionary<string, double> ProcessingRatePerSecondStats { get; private set; } = new();
        public Dictionary<string, double> SequencingRatePerSecondStats { get; private set; } = new();
        public double AverageRatePerSecond { get; private set; }

        private Perfs() { }

        public static async Task<Perfs> CreateAsync(IEnumerable<MyMessage> mms)
        {
            var instance = new Perfs();
            await instance.InitializeAsync(mms);
            return instance;
        }

        private async Task InitializeAsync(IEnumerable<MyMessage> mms)
        {
            // Move potentially heavy operations to Task.Run
            var (firstTime, lastTime) = await Task.Run(() => 
            {
                return mms.Any() ? (mms.Min(mm => mm.ProcessingAt), mms.Max(mm => mm.SequencingAt)) : (0L, 0L);
            });

            var diff = lastTime - firstTime;
            var spanInSecond = (double)diff / 1000.0;
            var count = (long)Math.Ceiling(spanInSecond);

            logger.Info($"firstTime={firstTime}, lastTime={lastTime}, diff={diff}, spanInSecond={spanInSecond}, count={count}");

            // Process buckets in parallel
            var tasks = new List<Task>();
            for (var i = 0; i < count; i++)
            {
                var index = i;
                tasks.Add(Task.Run(() =>
                {
                    var from = firstTime + (index * 1000);
                    var to = from + 1000;

                    var processingMatches = mms.Where(mm => mm.ProcessingAt >= from && mm.ProcessingAt < to);
                    lock (ProcessingRatePerSecond)
                    {
                        ProcessingRatePerSecond[index] = processingMatches.Count();
                    }

                    var sequencingMatches = mms.Where(mm => mm.SequencingAt >= from && mm.SequencingAt < to);
                    lock (SequencingRatePerSecond)
                    {
                        SequencingRatePerSecond[index] = sequencingMatches.Count();
                    }

                    logger.Info($"i={index}, from={from}, to={to}, processingMatches.Count()={processingMatches.Count()}, sequencingMatches.Count()={sequencingMatches.Count()}");
                }));
            }

            await Task.WhenAll(tasks);

            // Calculate stats
            await Task.Run(() =>
            {
                ProcessingRatePerSecondStats = ProcessingRatePerSecond.Values.Any() ? Stats.GetStats(ProcessingRatePerSecond.Values.Order()) : new();

                SequencingRatePerSecondStats = SequencingRatePerSecond.Values.Any() ? Stats.GetStats(SequencingRatePerSecond.Values.Order()) : new();

                AverageRatePerSecond = spanInSecond == 0.0 ? 0.0 : mms.Count() / spanInSecond;
            });
        }
    }
}