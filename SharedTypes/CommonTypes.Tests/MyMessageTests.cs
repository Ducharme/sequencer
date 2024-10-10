using System;
using Xunit;
using System.Text.Json;

namespace CommonTypes.Tests
{
    public class MyMessageTests
    {
        private readonly MyMessage _sampleMessage;

        public MyMessageTests()
        {
            _sampleMessage = new MyMessage
            {
                Sequence = 1,
                Name = "TestMessage",
                Payload = "TestPayload",
                Delay = 500,
                CreatedAt = 1715230115432,
                ProcessingAt = 1715230115638,
                ProcessedAt = 1715230116138,
                SequencingAt = 1715230116181,
                SavedAt = 1715230116427,
                SequencedAt = 1715230116953
            };
        }

        [Fact]
        public void GetDateTimeAsString_ShouldReturnFormattedDateTime()
        {
            long timestamp = 1715230115432; // 2024-05-08 15:35:15.432 UTC
            var utcDateTime = DateTimeOffset.FromUnixTimeMilliseconds(timestamp);
            string expected = utcDateTime.ToString("yyyy/MM/dd HH:mm:ss.fff");
            
            string result = MyMessage.GetDateTimeAsString(timestamp);
            
            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetTimeAsString_ShouldReturnFormattedTime()
        {
            long timestamp = 1715230115432; // 2024-05-08 15:35:15.432 UTC
            var utcDateTime = DateTimeOffset.FromUnixTimeMilliseconds(timestamp);
            string expected = utcDateTime.ToString("H:mm:ss.fff");
            
            string result = MyMessage.GetTimeAsString(timestamp);
            
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ToString_ShouldReturnShortString()
        {
            string expected = "1;TestMessage;TestPayload;500;1715230115432;1715230115638;1715230116138;1715230116181;1715230116427;1715230116953";
            
            string result = _sampleMessage.ToString();
            
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ToLongString_ShouldReturnDetailedString()
        {
            string expected = "Sequence:1, Name:TestMessage, Payload:TestPayload, Delay:500, CreatedAt:1715230115432, ProcessingAt:1715230115638, ProcessedAt:1715230116138, SequencingAt:1715230116181, SavedAt:1715230116427, SequencedAt:1715230116953";
            
            string result = _sampleMessage.ToLongString();
            
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ToShortString_ShouldReturnConciseString()
        {
            string expected = "1;TestMessage;TestPayload;500;1715230115432;1715230115638;1715230116138;1715230116181;1715230116427;1715230116953";
            
            string result = _sampleMessage.ToShortString();
            
            Assert.Equal(expected, result);
        }

        /*[Fact]
        public void ToDateString_ShouldReturnStringWithFormattedDates()
        {
            string expected = "Sequence:1, Name:TestMessage, Payload:TestPayload, Delay:500, CreatedAt:2024/05/08 15:35:15.432, ProcessingAt:2024/05/08 15:35:15.638, ProcessedAt:2024/05/08 15:35:16.138, SequencingAt:2024/05/08 15:35:16.181, SavedAt:2024/05/08 15:35:16.427, SequencedAt:2024/05/08 15:35:16.953";
            
            string result = _sampleMessage.ToDateString();
            
            Assert.Equal(expected, result);
        }*/

        /*[Fact]
        public void ToTimeString_ShouldReturnStringWithFormattedTimes()
        {
            string expected = "Sequence:1, Name:TestMessage, Payload:TestPayload, Delay:500, CreatedAt:15:35:15.432, ProcessingAt:15:35:15.638, ProcessedAt:15:35:16.138, SequencingAt:15:35:16.181, SavedAt:15:35:16.427, SequencedAt:15:35:16.953";
            
            string result = _sampleMessage.ToTimeString();
            
            Assert.Equal(expected, result);
        }*/

        [Fact]
        public void ToTinyString_ShouldReturnMinimalString()
        {
            string expected = "1;TestMessage;TestPayload;500";
            
            string result = _sampleMessage.ToTinyString();
            
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ToJson_ShouldReturnValidJsonString()
        {
            string expected = "{\"Sequence\":1,\"Name\":\"TestMessage\",\"Payload\":\"TestPayload\",\"Delay\":500,\"CreatedAt\":1715230115432,\"ProcessingAt\":1715230115638,\"ProcessedAt\":1715230116138,\"SequencingAt\":1715230116181,\"SavedAt\":1715230116427,\"SequencedAt\":1715230116953}";
            
            string result = _sampleMessage.ToJson();
            
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ToJson2_ShouldReturnValidJsonString()
        {
            Assert.NotNull(_sampleMessage);
            var json2 = _sampleMessage.ToJson2();
            Assert.NotNull(json2);
            var result = json2 as string;
            Assert.NotNull(result);

            var deserializedMessage = JsonSerializer.Deserialize<MyMessage>(result);
            
            Assert.Equal(_sampleMessage.Sequence, deserializedMessage!.Sequence);
            Assert.Equal(_sampleMessage.Name, deserializedMessage.Name);
            Assert.Equal(_sampleMessage.Payload, deserializedMessage.Payload);
            Assert.Equal(_sampleMessage.Delay, deserializedMessage.Delay);
            Assert.Equal(_sampleMessage.CreatedAt, deserializedMessage.CreatedAt);
            Assert.Equal(_sampleMessage.ProcessingAt, deserializedMessage.ProcessingAt);
            Assert.Equal(_sampleMessage.ProcessedAt, deserializedMessage.ProcessedAt);
            Assert.Equal(_sampleMessage.SequencingAt, deserializedMessage.SequencingAt);
            Assert.Equal(_sampleMessage.SavedAt, deserializedMessage.SavedAt);
            Assert.Equal(_sampleMessage.SequencedAt, deserializedMessage.SequencedAt);
        }
    }
}