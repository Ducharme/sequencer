
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
        public Dictionary<string, double> ProcessingRatePerSecondStats { get; }
        public Dictionary<string, double> SequencingRatePerSecondStats { get; }
        public double AverageRatePerSecond { get; }

        public Perfs(IEnumerable<MyMessage> mms)
        {
            // Long contains milliseconds (11 digits)
            var firstTime = mms.Min(mm => mm.ProcessingAt);
            var lastTime = mms.Max(mm => mm.SequencedAt);
            
            // Bucket processing per second
            var diff = lastTime - firstTime;
            var spanInSecond = (double)diff / 1000.0;
            var count = (long)Math.Ceiling(spanInSecond);

            logger.Info($"firstTime={firstTime}, lastTime={lastTime}, diff={diff}, spanInSecond={spanInSecond}, count={count}");
            for (var i=0; i < count; i++)
            {
                var from = firstTime + (i * 1000);
                var to = from + 1000;

                var processingMatches = mms.Where(mm => mm.ProcessingAt >= from && mm.ProcessedAt < to);
                ProcessingRatePerSecond.Add(i, processingMatches.Count());

                var sequencingMatches = mms.Where(mm => mm.SequencingAt >= from && mm.SequencedAt < to);
                SequencingRatePerSecond.Add(i, sequencingMatches.Count());

                logger.Info($"i={i}, from={from}, to={to}, processingMatches.Count()={processingMatches.Count()}, sequencingMatches.Count()={sequencingMatches.Count()}");
            }

            ProcessingRatePerSecondStats = ProcessingRatePerSecond.Values.Any() ? Stats.GetStats(ProcessingRatePerSecond.Values.ToList()) : new ();
            SequencingRatePerSecondStats = SequencingRatePerSecond.Values.Any() ? Stats.GetStats(SequencingRatePerSecond.Values.ToList()) : new ();
            AverageRatePerSecond = spanInSecond == 0.0 ? 0.0 : mms.Count() / spanInSecond;
        }
    }
}