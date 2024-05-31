namespace CommonTypes
{
    public static class DateTimeHelper
    {
        public static readonly DateTime DateTimeMin = new DateTime(0);

        public static DateTime GetDateTime(long timestamp)
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(timestamp).UtcDateTime;
        }

        public static DateTime GetDateTimeFromObject(object? obj)
        {
            var dt = DateTimeMin;
            if (obj != null)
            {
                if (obj is DateTime || obj.GetType() == typeof(DateTime))
                {
                    dt = (DateTime)obj;
                }
                else if (obj is DateTimeOffset || obj.GetType() == typeof(DateTimeOffset))
                {
                    var dto = (DateTimeOffset)obj;
                    dt = dto.DateTime;
                }
                else if (obj is long || obj.GetType() == typeof(long))
                {
                    var lng = (long)obj;
                    dt = DateTimeOffset.FromUnixTimeMilliseconds(lng).UtcDateTime;
                }
            }

            return dt;
        }

        public static long GetTimestamp(DateTime dt)
        {
            // Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            return (long)(dt - DateTime.UnixEpoch).TotalMilliseconds;
        }

        public static long GetEpochMilliseconds(object obj)
        {
            long ret = 0;
            if (obj != null)
            {
                if (obj is DateTime || obj.GetType() == typeof(DateTime))
                {
                    var dt = (DateTime)obj;
                    ret = dt.Ticks / 10000;
                }
            }
            return ret;
        }
    }
}