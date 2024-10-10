using System;
using Xunit;
using CommonTypes;

namespace CommonTypes.Tests
{
    public class DateTimeHelperTests
    {
        [Fact]
        public void DateTimeMin_ShouldBeMinimumDateTime()
        {
            Assert.Equal(new DateTime(0), DateTimeHelper.DateTimeMin);
        }

        [Fact]
        public void GetDateTime_ShouldConvertTimestampToDateTime()
        {
            long timestamp = 1609459200000; // 2021-01-01 00:00:00 UTC
            DateTime expected = new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            DateTime result = DateTimeHelper.GetDateTime(timestamp);

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("not a datetime")]
        public void GetDateTimeFromObject_ShouldReturnDateTimeMinForInvalidInput(object input)
        {
            DateTime result = DateTimeHelper.GetDateTimeFromObject(input);
            Assert.Equal(DateTimeHelper.DateTimeMin, result);
        }

        [Fact]
        public void GetDateTimeFromObject_ShouldHandleDateTime()
        {
            DateTime input = new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime result = DateTimeHelper.GetDateTimeFromObject(input);
            Assert.Equal(input, result);
        }

        [Fact]
        public void GetDateTimeFromObject_ShouldHandleDateTimeOffset()
        {
            DateTimeOffset input = new DateTimeOffset(2021, 1, 1, 0, 0, 0, TimeSpan.Zero);
            DateTime result = DateTimeHelper.GetDateTimeFromObject(input);
            Assert.Equal(input.DateTime, result);
        }

        [Fact]
        public void GetDateTimeFromObject_ShouldHandleLong()
        {
            long input = 1609459200000; // 2021-01-01 00:00:00 UTC
            DateTime expected = new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime result = DateTimeHelper.GetDateTimeFromObject(input);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetTimestamp_ShouldConvertDateTimeToTimestamp()
        {
            DateTime input = new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            long expected = 1609459200000; // 2021-01-01 00:00:00 UTC

            long result = DateTimeHelper.GetTimestamp(input);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetEpochMilliseconds_ShouldReturnZeroForNullInput()
        {
            long result = DateTimeHelper.GetEpochMilliseconds(new object());
            Assert.Equal(0, result);
        }

        [Fact]
        public void GetEpochMilliseconds_ShouldConvertDateTimeToEpochMilliseconds()
        {
            DateTime input = new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            long expected = 63745056000000; // 2021-01-01 00:00:00 UTC

            long result = DateTimeHelper.GetEpochMilliseconds(input);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetEpochMilliseconds_ShouldReturnZeroForNonDateTimeInput()
        {
            long result = DateTimeHelper.GetEpochMilliseconds("not a datetime");
            Assert.Equal(0, result);
        }
    }
}