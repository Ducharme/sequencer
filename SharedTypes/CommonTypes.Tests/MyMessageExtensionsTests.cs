using System;
using Xunit;
using System.Text.Json;

namespace CommonTypes.Tests
{
    public class MyMessageExtensionsTests
    {
        private readonly MyMessage _sampleMessage;
        private readonly string _sampleJson;
        private readonly string _sampleShortString;

        public MyMessageExtensionsTests()
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

            _sampleJson = "{\"Sequence\":1,\"Name\":\"TestMessage\",\"Payload\":\"TestPayload\",\"Delay\":500,\"CreatedAt\":1715230115432,\"ProcessingAt\":1715230115638,\"ProcessedAt\":1715230116138,\"SequencingAt\":1715230116181,\"SavedAt\":1715230116427,\"SequencedAt\":1715230116953}";
            _sampleShortString = "1;TestMessage;TestPayload;500;1715230115432;1715230115638;1715230116138;1715230116181;1715230116427;1715230116953";
        }

        [Fact]
        public void ToJson_ShouldSerializeMessageToJson()
        {
            string result = _sampleMessage.ToJson();

            Assert.Equal(_sampleJson, result);
        }

        [Fact]
        public void FromJson_ShouldDeserializeJsonToMessage()
        {
            MyMessage? result = _sampleJson.FromJson();

            Assert.NotNull(result);
            Assert.Equal(_sampleMessage.Sequence, result.Sequence);
            Assert.Equal(_sampleMessage.Name, result.Name);
            Assert.Equal(_sampleMessage.Payload, result.Payload);
            Assert.Equal(_sampleMessage.Delay, result.Delay);
            Assert.Equal(_sampleMessage.CreatedAt, result.CreatedAt);
            Assert.Equal(_sampleMessage.ProcessingAt, result.ProcessingAt);
            Assert.Equal(_sampleMessage.ProcessedAt, result.ProcessedAt);
            Assert.Equal(_sampleMessage.SequencingAt, result.SequencingAt);
            Assert.Equal(_sampleMessage.SavedAt, result.SavedAt);
            Assert.Equal(_sampleMessage.SequencedAt, result.SequencedAt);
        }

        [Fact]
        public void FromShortString_ShouldCreateMessageFromShortString()
        {
            MyMessage? result = _sampleShortString.FromShortString();

            Assert.NotNull(result);
            Assert.Equal(_sampleMessage.Sequence, result.Sequence);
            Assert.Equal(_sampleMessage.Name, result.Name);
            Assert.Equal(_sampleMessage.Payload, result.Payload);
            Assert.Equal(_sampleMessage.Delay, result.Delay);
            Assert.Equal(_sampleMessage.CreatedAt, result.CreatedAt);
            Assert.Equal(_sampleMessage.ProcessingAt, result.ProcessingAt);
            Assert.Equal(_sampleMessage.ProcessedAt, result.ProcessedAt);
            Assert.Equal(_sampleMessage.SequencingAt, result.SequencingAt);
            Assert.Equal(_sampleMessage.SavedAt, result.SavedAt);
            Assert.Equal(_sampleMessage.SequencedAt, result.SequencedAt);
        }

        [Fact]
        public void FromShortString_ShouldHandleInvalidLongValues()
        {
            string invalidShortString = "1;TestMessage;TestPayload;500;invalid;invalid;invalid;invalid;invalid;invalid";
            MyMessage? result = invalidShortString.FromShortString();

            Assert.NotNull(result);
            Assert.Equal(1, result.Sequence);
            Assert.Equal("TestMessage", result.Name);
            Assert.Equal("TestPayload", result.Payload);
            Assert.Equal(500, result.Delay);
            Assert.Equal(0, result.CreatedAt);
            Assert.Equal(0, result.ProcessingAt);
            Assert.Equal(0, result.ProcessedAt);
            Assert.Equal(0, result.SequencingAt);
            Assert.Equal(0, result.SavedAt);
            Assert.Equal(0, result.SequencedAt);
        }

        [Fact]
        public void FromShortString_ShouldHandleIncompleteString()
        {
            string incompleteString = "1;TestMessage;TestPayload;500";
            Assert.Throws<IndexOutOfRangeException>(() => incompleteString.FromShortString());
        }
    }
}