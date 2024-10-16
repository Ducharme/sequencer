using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommonTypes;
using Moq;
using RedisAccessLayer;
using StackExchange.Redis;
using Xunit;

public class ProcessedListToSequencedListListenerTests : IDisposable
{
    private readonly Mock<IRedisConnectionManager> _mockRedisConnectionManager;
    private readonly Mock<ISyncLock> _mockSyncLock;
    private readonly ProcessedListToSequencedListListener _listener;

    public ProcessedListToSequencedListListenerTests()
    {
        var behavior = MockBehavior.Strict;
        _mockRedisConnectionManager = new Mock<IRedisConnectionManager>(behavior);
        _mockSyncLock = new Mock<ISyncLock>(behavior);
        _listener = new ProcessedListToSequencedListListener(_mockRedisConnectionManager.Object, _mockSyncLock.Object);
    }

    [Fact]
    public async Task FromProcessedToSequenced_ShouldProcessMessagesCorrectly()
    {
        // Arrange
        var messages = new List<MyStreamMessage>
        {
            new MyStreamMessage { Sequence = 1, StreamId = "1-0", Name = "Test1", Payload = "Payload1" },
            new MyStreamMessage { Sequence = 2, StreamId = "2-0", Name = "Test2", Payload = "Payload2" }
        };

        _mockRedisConnectionManager.Setup(m => m.StreamAddListLeftPushStreamDeletePublishInTransactionAsync(
            It.IsAny<RedisKey>(), It.IsAny<RedisKey>(), It.IsAny<List<Tuple<string, NameValueEntry[]>>>(),
            It.IsAny<RedisKey>(), It.IsAny<RedisValue[]>(), It.IsAny<RedisChannel>(), It.IsAny<RedisValue>()))
            .ReturnsAsync(true);

        // Act
        var result = await _listener.FromProcessedToSequenced(messages);

        // Assert
        Assert.True(result.Item1);
        Assert.Equal("NA", result.Item2);
        Assert.Equal(2, result.Item3);

        _mockRedisConnectionManager.Verify(m => m.StreamAddListLeftPushStreamDeletePublishInTransactionAsync(
            It.IsAny<RedisKey>(), It.IsAny<RedisKey>(), It.IsAny<List<Tuple<string, NameValueEntry[]>>>(),
            It.IsAny<RedisKey>(), It.IsAny<RedisValue[]>(), It.IsAny<RedisChannel>(), It.IsAny<RedisValue>()), Times.Once);
    }

    [Fact]
    public async Task FromProcessedToSequenced_EmptyList_ShouldThrowException()
    {
        // Arrange
        var messages = new List<MyStreamMessage>();

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _listener.FromProcessedToSequenced(messages)
        );

        // Assert
        Assert.Contains("Cannot process an empty list of messages", exception.Message);
    }

    [Fact]
    public async Task FromProcessedToSequenced_LargeListOfMessages_ShouldProcessCorrectly()
    {
        // Arrange
        var messages = new List<MyStreamMessage>();
        for (int i = 1; i <= 1000; i++)
        {
            messages.Add(new MyStreamMessage { Sequence = i, StreamId = $"{i}-0", Name = $"Test{i}", Payload = $"Payload{i}" });
        }

        _mockRedisConnectionManager.Setup(m => m.StreamAddListLeftPushStreamDeletePublishInTransactionAsync(
            It.IsAny<RedisKey>(), It.IsAny<RedisKey>(), It.IsAny<List<Tuple<string, NameValueEntry[]>>>(),
            It.IsAny<RedisKey>(), It.IsAny<RedisValue[]>(), It.IsAny<RedisChannel>(), It.IsAny<RedisValue>()))
            .ReturnsAsync(true);

        // Act
        var result = await _listener.FromProcessedToSequenced(messages);

        // Assert
        Assert.True(result.Item1);
        Assert.Equal("NA", result.Item2);
        Assert.Equal(1000, result.Item3);
    }

    [Fact]
    public async Task FromProcessedToSequenced_TransactionFails_ShouldReturnFalse()
    {
        // Arrange
        var messages = new List<MyStreamMessage>
        {
            new MyStreamMessage { Sequence = 1, StreamId = "1-0", Name = "Test1", Payload = "Payload1" }
        };

        _mockRedisConnectionManager.Setup(m => m.StreamAddListLeftPushStreamDeletePublishInTransactionAsync(
            It.IsAny<RedisKey>(), It.IsAny<RedisKey>(), It.IsAny<List<Tuple<string, NameValueEntry[]>>>(),
            It.IsAny<RedisKey>(), It.IsAny<RedisValue[]>(), It.IsAny<RedisChannel>(), It.IsAny<RedisValue>()))
            .ReturnsAsync(false);

        // Act
        var result = await _listener.FromProcessedToSequenced(messages);

        // Assert
        Assert.False(result.Item1);
        Assert.Equal("NA", result.Item2);
        Assert.Equal(1, result.Item3);
    }

    [Fact]
    public async Task FromProcessedToSequenced_RedisException_ShouldThrowAndNotUpdateLastProcessedSequenceId()
    {
        // Arrange
        var messages = new List<MyStreamMessage>
        {
            new MyStreamMessage { Sequence = 1, StreamId = "1-0", Name = "Test1", Payload = "Payload1" }
        };

        _mockRedisConnectionManager.Setup(m => m.StreamAddListLeftPushStreamDeletePublishInTransactionAsync(
            It.IsAny<RedisKey>(), It.IsAny<RedisKey>(), It.IsAny<List<Tuple<string, NameValueEntry[]>>>(),
            It.IsAny<RedisKey>(), It.IsAny<RedisValue[]>(), It.IsAny<RedisChannel>(), It.IsAny<RedisValue>()))
            .ThrowsAsync(new RedisException("Redis error"));

        // Act & Assert
        await Assert.ThrowsAsync<RedisException>(() => _listener.FromProcessedToSequenced(messages));
        Assert.Null(_listener.LastProcessedSequenceId);
    }

    [Fact]
    public async Task FromProcessedToSequenced_NonSequentialMessages_ShouldProcessCorrectly()
    {
        // Arrange
        var messages = new List<MyStreamMessage>
        {
            new MyStreamMessage { Sequence = 3, StreamId = "3-0", Name = "Test3", Payload = "Payload3" },
            new MyStreamMessage { Sequence = 1, StreamId = "1-0", Name = "Test1", Payload = "Payload1" },
            new MyStreamMessage { Sequence = 4, StreamId = "4-0", Name = "Test4", Payload = "Payload4" },
            new MyStreamMessage { Sequence = 2, StreamId = "2-0", Name = "Test2", Payload = "Payload2" }
        };

        _mockRedisConnectionManager.Setup(m => m.StreamAddListLeftPushStreamDeletePublishInTransactionAsync(
            It.IsAny<RedisKey>(), It.IsAny<RedisKey>(), It.IsAny<List<Tuple<string, NameValueEntry[]>>>(),
            It.IsAny<RedisKey>(), It.IsAny<RedisValue[]>(), It.IsAny<RedisChannel>(), It.IsAny<RedisValue>()))
            .ReturnsAsync(true);

        // Act
        var result = await _listener.FromProcessedToSequenced(messages);

        // Assert
        Assert.True(result.Item1);
        Assert.Equal("NA", result.Item2);
        Assert.Equal(4, result.Item3);
    }

    [Fact]
    public async Task FromProcessedToSequenced_MessageWithNullStreamId_ShouldSkipDeletion()
    {
        // Arrange
        var messages = new List<MyStreamMessage>
        {
            new MyStreamMessage { Sequence = 1, StreamId = null, Name = "Test1", Payload = "Payload1" },
            new MyStreamMessage { Sequence = 2, StreamId = "2-0", Name = "Test2", Payload = "Payload2" }
        };

        _mockRedisConnectionManager.Setup(m => m.StreamAddListLeftPushStreamDeletePublishInTransactionAsync(
            It.IsAny<RedisKey>(), It.IsAny<RedisKey>(), It.IsAny<List<Tuple<string, NameValueEntry[]>>>(),
            It.IsAny<RedisKey>(), It.Is<RedisValue[]>(arr => arr.Length == 1), It.IsAny<RedisChannel>(), It.IsAny<RedisValue>()))
            .ReturnsAsync(true);

        // Act
        var result = await _listener.FromProcessedToSequenced(messages);

        // Assert
        Assert.True(result.Item1);
        Assert.Equal("NA", result.Item2);
        Assert.Equal(2, result.Item3);
        _mockRedisConnectionManager.Verify(m => m.StreamAddListLeftPushStreamDeletePublishInTransactionAsync(
            It.IsAny<RedisKey>(), It.IsAny<RedisKey>(), It.IsAny<List<Tuple<string, NameValueEntry[]>>>(),
            It.IsAny<RedisKey>(), It.Is<RedisValue[]>(arr => arr.Length == 1), It.IsAny<RedisChannel>(), It.IsAny<RedisValue>()), Times.Once);
    }

    [Fact]
    public async Task FromProcessedToSequenced_DuplicateSequenceIds_ShouldProcessAllMessages()
    {
        // Arrange
        var messages = new List<MyStreamMessage>
        {
            new MyStreamMessage { Sequence = 1, StreamId = "1-0", Name = "Test1", Payload = "Payload1" },
            new MyStreamMessage { Sequence = 1, StreamId = "1-1", Name = "Test1Duplicate", Payload = "Payload1Duplicate" },
            new MyStreamMessage { Sequence = 2, StreamId = "2-0", Name = "Test2", Payload = "Payload2" }
        };

        _mockRedisConnectionManager.Setup(m => m.StreamAddListLeftPushStreamDeletePublishInTransactionAsync(
            It.IsAny<RedisKey>(), It.IsAny<RedisKey>(), It.Is<List<Tuple<string, NameValueEntry[]>>>(list => list.Count == 3),
            It.IsAny<RedisKey>(), It.Is<RedisValue[]>(arr => arr.Length == 3), It.IsAny<RedisChannel>(), It.IsAny<RedisValue>()))
            .ReturnsAsync(true);

        // Act
        var result = await _listener.FromProcessedToSequenced(messages);

        // Assert
        Assert.True(result.Item1);
        Assert.Equal("NA", result.Item2);
        Assert.Equal(2, result.Item3); // Highest sequence ID
    }

    [Fact]
    public async Task FromProcessedToSequenced_VeryLargePayload_ShouldProcessSuccessfully()
    {
        // Arrange
        var largePayload = new string('X', 1000000); // 1MB payload
        var messages = new List<MyStreamMessage>
        {
            new MyStreamMessage { Sequence = 1, StreamId = "1-0", Name = "LargeMessage", Payload = largePayload }
        };

        _mockRedisConnectionManager.Setup(m => m.StreamAddListLeftPushStreamDeletePublishInTransactionAsync(
            It.IsAny<RedisKey>(), It.IsAny<RedisKey>(), It.Is<List<Tuple<string, NameValueEntry[]>>>(list => list[0].Item2.Any(nv => nv.Name == "Payload" && nv.Value.ToString().Length == 1000000)),
            It.IsAny<RedisKey>(), It.IsAny<RedisValue[]>(), It.IsAny<RedisChannel>(), It.IsAny<RedisValue>()))
            .ReturnsAsync(true);

        // Act
        var result = await _listener.FromProcessedToSequenced(messages);

        // Assert
        Assert.True(result.Item1);
        Assert.Equal("NA", result.Item2);
        Assert.Equal(1, result.Item3);
    }

    [Fact]
    public async Task FromProcessedToSequenced_SequenceGap_ShouldProcessAvailableMessages()
    {
        // Arrange
        var messages = new List<MyStreamMessage>
        {
            new MyStreamMessage { Sequence = 1, StreamId = "1-0", Name = "Test1", Payload = "Payload1" },
            new MyStreamMessage { Sequence = 2, StreamId = "2-0", Name = "Test2", Payload = "Payload2" },
            new MyStreamMessage { Sequence = 4, StreamId = "4-0", Name = "Test4", Payload = "Payload4" } // Note the gap
        };

        _mockRedisConnectionManager.Setup(m => m.StreamAddListLeftPushStreamDeletePublishInTransactionAsync(
            It.IsAny<RedisKey>(), It.IsAny<RedisKey>(), It.Is<List<Tuple<string, NameValueEntry[]>>>(list => list.Count == 3),
            It.IsAny<RedisKey>(), It.Is<RedisValue[]>(arr => arr.Length == 3), It.IsAny<RedisChannel>(), It.IsAny<RedisValue>()))
            .ReturnsAsync(true);

        // Act
        var result = await _listener.FromProcessedToSequenced(messages);

        // Assert
        Assert.True(result.Item1);
        Assert.Equal("NA", result.Item2);
        Assert.Equal(4, result.Item3); // Highest sequence ID
    }

    [Fact]
    public async Task FromProcessedToSequenced_ConcurrentCalls_ShouldProcessCorrectly()
    {
        // Arrange
        var messages1 = new List<MyStreamMessage>
        {
            new MyStreamMessage { Sequence = 1, StreamId = "1-0", Name = "Test1", Payload = "Payload1" },
            new MyStreamMessage { Sequence = 2, StreamId = "2-0", Name = "Test2", Payload = "Payload2" }
        };

        var messages2 = new List<MyStreamMessage>
        {
            new MyStreamMessage { Sequence = 3, StreamId = "3-0", Name = "Test3", Payload = "Payload3" },
            new MyStreamMessage { Sequence = 4, StreamId = "4-0", Name = "Test4", Payload = "Payload4" }
        };

        _mockRedisConnectionManager.Setup(m => m.StreamAddListLeftPushStreamDeletePublishInTransactionAsync(
            It.IsAny<RedisKey>(), It.IsAny<RedisKey>(), It.IsAny<List<Tuple<string, NameValueEntry[]>>>(),
            It.IsAny<RedisKey>(), It.IsAny<RedisValue[]>(), It.IsAny<RedisChannel>(), It.IsAny<RedisValue>()))
            .ReturnsAsync(true);

        // Act
        var task1 = _listener.FromProcessedToSequenced(messages1);
        var task2 = _listener.FromProcessedToSequenced(messages2);

        await Task.WhenAll(task1, task2);

        // Assert
        Assert.True(task1.Result.Item1);
        Assert.True(task2.Result.Item1);
        Assert.Equal(4, Math.Max(task1.Result.Item3, task2.Result.Item3));
    }

    public void Dispose()
    {
        _mockRedisConnectionManager.Reset();
        _mockSyncLock.Reset();
        _listener.Dispose();
    }
}