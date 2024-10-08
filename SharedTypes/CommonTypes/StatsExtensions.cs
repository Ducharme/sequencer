namespace CommonTypes
{
    public static class StatsExtensions
    {
        public static Dictionary<string, long> GetSequenceAndMax(this IEnumerable<MyMessageStats> stats, Func<MyMessageStats, long> propertySelector)
        {
            if (stats == null || !stats.Any() || propertySelector == null)
            {
                return [];
            }

            var maxTime = stats.Max(propertySelector);
            var seq = stats.First(stat => propertySelector(stat) == maxTime).Sequence;
            var dic = new Dictionary<string, long>
            {
                { "max", maxTime },
                { "seq", seq }
            };
            return dic;
        }

        public static Dictionary<string, long> GetSequenceAndMin(this IEnumerable<MyMessageStats> stats, Func<MyMessageStats, long> propertySelector)
        {
            if (stats == null || !stats.Any() || propertySelector == null)
            {
                return [];
            }

            var minTime = stats.Min(propertySelector);
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