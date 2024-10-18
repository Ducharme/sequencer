using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommonTypes;
using Moq;
using RedisAccessLayer;
using StackExchange.Redis;
using Xunit;

public class ProcessedListToSequencedListListenerComplexTests : IDisposable
{
    private readonly Mock<IRedisConnectionManager> _mockRedisConnectionManager;
    private readonly Mock<ISyncLock> _mockSyncLock;
    private readonly ProcessedListToSequencedListListener _listener;

    public ProcessedListToSequencedListListenerComplexTests()
    {
        //CommonServiceLib.Program.ConfigureLogging();
        /*var behavior = MockBehavior.Strict;*/
        _mockRedisConnectionManager = new Mock<IRedisConnectionManager>(/*behavior*/);
        _mockSyncLock = new Mock<ISyncLock>(/*behavior*/);
        _listener = new ProcessedListToSequencedListListener(_mockRedisConnectionManager.Object, _mockSyncLock.Object);
    }

    [Fact]
    public async Task ListenForPendingMessages_ShouldHandleMessagesCorrectly()
    {
        // Arrange
        var handlerCalled = false;
        Func<List<MyStreamMessage>, Task<bool>> handler = async (messages) =>
        {
            await Task.Delay(1);
            handlerCalled = true;
            return true;
        };

        _mockSyncLock.Setup(l => l.AcquireLock()).ReturnsAsync(true);
        _mockSyncLock.Setup(l => l.RemainingLockTime).Returns(TimeSpan.FromSeconds(1));

        _mockRedisConnectionManager.Setup(m => m.SubscribeAsync()).Returns(Task.CompletedTask);
        _mockRedisConnectionManager.Setup(m => m.StreamReadAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>()))
            .ReturnsAsync(new[]
            {
                new StreamEntry("1-0", new NameValueEntry[]
                {
                    new NameValueEntry("Sequence", "1"),
                    new NameValueEntry("Name", "Test1"),
                    new NameValueEntry("Payload", "Payload1")
                })
            });

        // Act
        Task listenTask = Task.Run(() => _listener.ListenForPendingMessages(handler));

        // Simulate stopping the listener after a short delay
        await Task.Delay(200);
        _listener.StopListening();

        await listenTask;

        // Assert
        Assert.True(handlerCalled);
        _mockRedisConnectionManager.Verify(m => m.SubscribeAsync(), Times.Once);
        _mockRedisConnectionManager.Verify(m => m.StreamReadAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ListenForPendingMessages_NoMessages_ShouldNotCallHandler()
    {
        // Arrange
        var handlerCalled = false;
        Func<List<MyStreamMessage>, Task<bool>> handler = async (messages) =>
        {
            await Task.Delay(1);
            handlerCalled = true;
            return true;
        };

        _mockSyncLock.Setup(l => l.AcquireLock()).ReturnsAsync(true);
        _mockSyncLock.Setup(l => l.RemainingLockTime).Returns(TimeSpan.FromSeconds(1));

        _mockRedisConnectionManager.Setup(m => m.SubscribeAsync()).Returns(Task.CompletedTask);
        _mockRedisConnectionManager.Setup(m => m.StreamReadAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>()))
            .ReturnsAsync(Array.Empty<StreamEntry>());

        // Act
        Task listenTask = Task.Run(() => _listener.ListenForPendingMessages(handler));

        await Task.Delay(150);
        _listener.StopListening();

        await listenTask;

        // Assert
        Assert.False(handlerCalled);
    }


    [Fact]
    public async Task ListenForPendingMessages_FailToAcquireLock_ShouldRetryAndEventuallyAcquireLock()
    {
        // Arrange
        var handlerCalled = false;
        Func<List<MyStreamMessage>, Task<bool>> handler = async (messages) =>
        {
            await Task.Delay(1);
            handlerCalled = true;
            return true;
        };

        var lockAcquired = false;
        _mockSyncLock.Setup(l => l.AcquireLock()).ReturnsAsync(() =>
        {
            if (!lockAcquired)
            {
                lockAcquired = true;
                return false;
            }
            return true;
        });
        _mockSyncLock.Setup(l => l.RemainingLockTime).Returns(TimeSpan.FromSeconds(1));

        _mockRedisConnectionManager.Setup(m => m.SubscribeAsync()).Returns(Task.CompletedTask);
        _mockRedisConnectionManager.Setup(m => m.StreamReadAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>()))
            .ReturnsAsync(new[]
            {
                new StreamEntry("1-0", new NameValueEntry[]
                {
                    new NameValueEntry("Sequence", "1"),
                    new NameValueEntry("Name", "Test1"),
                    new NameValueEntry("Payload", "Payload1")
                })
            });

        // Act
        Task listenTask = Task.Run(() => _listener.ListenForPendingMessages(handler));

        await Task.Delay(200);
        _listener.StopListening();

        await listenTask;

        // Assert
        Assert.True(handlerCalled);
        _mockSyncLock.Verify(l => l.AcquireLock(), Times.AtLeast(2));
    }

    [Fact]
    public async Task ListenForPendingMessages_RedisTimeoutFailure_ShouldAttemptReconnect()
    {
        // Arrange
        var handlerCalled = false;
        Func<List<MyStreamMessage>, Task<bool>> handler = async (messages) =>
        {
            await Task.Delay(1);
            handlerCalled = true;
            return true;
        };

        _mockSyncLock.Setup(l => l.AcquireLock()).ReturnsAsync(true);
        _mockSyncLock.Setup(l => l.RemainingLockTime).Returns(TimeSpan.FromSeconds(1));

        var timeoutOccurred = false;
        _mockRedisConnectionManager.Setup(m => m.SubscribeAsync()).Returns(() =>
        {
            if (!timeoutOccurred)
            {
                timeoutOccurred = true;
                throw new RedisTimeoutException("Connection timeout", CommandStatus.Unknown);
            }
            return Task.CompletedTask;
        });
        _mockRedisConnectionManager.Setup(m => m.Reconnect()).ReturnsAsync(true);
        _mockRedisConnectionManager.Setup(m => m.StreamReadAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>()))
            .ReturnsAsync(new[]
            {
                new StreamEntry("1-0", new NameValueEntry[]
                {
                    new NameValueEntry("Sequence", "1"),
                    new NameValueEntry("Name", "Test1"),
                    new NameValueEntry("Payload", "Payload1")
                })
            });

        // Act
        Task listenTask = Task.Run(() => _listener.ListenForPendingMessages(handler));

        await Task.Delay(1200); // LongWaitTime is 1000 ms
        _listener.StopListening();

        await listenTask;

        // Assert
        Assert.True(handlerCalled);
        _mockRedisConnectionManager.Verify(m => m.Reconnect(), Times.Once);
    }

    [Fact]
    public async Task ListenForPendingMessages_RedisConnectionFailure_ShouldNotAttemptReconnect()
    {
        // Arrange
        var handlerCalled = false;
        Func<List<MyStreamMessage>, Task<bool>> handler = async (messages) =>
        {
            await Task.Delay(1);
            handlerCalled = true;
            return true;
        };

        _mockSyncLock.Setup(l => l.AcquireLock()).ReturnsAsync(true);
        _mockSyncLock.Setup(l => l.RemainingLockTime).Returns(TimeSpan.FromSeconds(1));

        var connectionFailed = false;
        _mockRedisConnectionManager.Setup(m => m.SubscribeAsync()).Returns(() =>
        {
            if (!connectionFailed)
            {
                connectionFailed = true;
                throw new RedisConnectionException(ConnectionFailureType.InternalFailure, "Connection failed");
            }
            return Task.CompletedTask;
        });
        _mockRedisConnectionManager.Setup(m => m.Reconnect()).ReturnsAsync(true);
        _mockRedisConnectionManager.Setup(m => m.StreamReadAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>()))
            .ReturnsAsync(new[]
            {
                new StreamEntry("1-0", new NameValueEntry[]
                {
                    new NameValueEntry("Sequence", "1"),
                    new NameValueEntry("Name", "Test1"),
                    new NameValueEntry("Payload", "Payload1")
                })
            });

        // Act
        Task listenTask = Task.Run(() => _listener.ListenForPendingMessages(handler));

        await Task.Delay(1200);
        _listener.StopListening();

        await listenTask;

        // Assert
        Assert.True(handlerCalled);
        _mockRedisConnectionManager.Verify(m => m.Reconnect(), Times.Never());
    }

    [Fact]
    public async Task ListenForPendingMessages_HandlerThrowsException_ShouldStopListening()
    {
        // Arrange
        var handlerCallCount = 0;
        Func<List<MyStreamMessage>, Task<bool>> handler = async (messages) =>
        {
            await Task.Delay(1);
            handlerCallCount++;
            if (handlerCallCount == 1)
            {
                throw new Exception("Handler error");
            }
            return true;
        };

        _mockSyncLock.Setup(l => l.AcquireLock()).ReturnsAsync(true);
        _mockSyncLock.Setup(l => l.RemainingLockTime).Returns(TimeSpan.FromSeconds(1));

        _mockRedisConnectionManager.Setup(m => m.SubscribeAsync()).Returns(Task.CompletedTask);
        _mockRedisConnectionManager.Setup(m => m.StreamReadAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>()))
            .ReturnsAsync(new[]
            {
                new StreamEntry("1-0", new NameValueEntry[]
                {
                    new NameValueEntry("Sequence", "1"),
                    new NameValueEntry("Name", "Test1"),
                    new NameValueEntry("Payload", "Payload1")
                })
            });

        // Act
        Task listenTask = Task.Run(() => _listener.ListenForPendingMessages(handler));

        await Task.Delay(250);
        _listener.StopListening();

        await listenTask;

        // Assert
        Assert.Equal(1, handlerCallCount);
    }

    [Fact]
    public async Task ListenForPendingMessages_LockExpired_ShouldReacquireLock()
    {
        // Arrange
        var handlerCalled = false;
        Func<List<MyStreamMessage>, Task<bool>> handler = async (messages) =>
        {
            await Task.Delay(1);
            handlerCalled = true;
            return true;
        };

        var lockExpired = false;
        _mockSyncLock.Setup(l => l.AcquireLock()).ReturnsAsync(true);
        _mockSyncLock.Setup(l => l.RemainingLockTime).Returns(() =>
        {
            if (!lockExpired)
            {
                lockExpired = true;
                return TimeSpan.Zero;
            }
            return TimeSpan.FromSeconds(1);
        });
        _mockSyncLock.Setup(l => l.ExtendLock(It.IsAny<TimeSpan>())).ReturnsAsync(true);

        _mockRedisConnectionManager.Setup(m => m.SubscribeAsync()).Returns(Task.CompletedTask);
        _mockRedisConnectionManager.Setup(m => m.StreamReadAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>()))
            .ReturnsAsync(new[]
            {
                new StreamEntry("1-0", new NameValueEntry[]
                {
                    new NameValueEntry("Sequence", "1"),
                    new NameValueEntry("Name", "Test1"),
                    new NameValueEntry("Payload", "Payload1")
                })
            });

        // Act
        Task listenTask = Task.Run(() => _listener.ListenForPendingMessages(handler));

        await Task.Delay(200);
        _listener.StopListening();

        await listenTask;

        // Assert
        Assert.True(handlerCalled);
        _mockSyncLock.Verify(l => l.ExtendLock(It.IsAny<TimeSpan>()), Times.Once);
    }

    [Fact]
    public async Task ListenForPendingMessages_StreamReadThrowsException_ShouldContinueListening()
    {
        // Arrange
        var handlerCallCount = 0;
        Func<List<MyStreamMessage>, Task<bool>> handler = async (messages) =>
        {
            await Task.Delay(1);
            handlerCallCount++;
            return true;
        };

        _mockSyncLock.Setup(l => l.AcquireLock()).ReturnsAsync(true);
        _mockSyncLock.Setup(l => l.RemainingLockTime).Returns(TimeSpan.FromSeconds(10));

        var exceptionThrown = false;
        _mockRedisConnectionManager.Setup(m => m.SubscribeAsync()).Returns(Task.CompletedTask);
        _mockRedisConnectionManager.Setup(m => m.StreamReadAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>()))
            .Returns(() =>
            {
                if (!exceptionThrown)
                {
                    exceptionThrown = true;
                    throw new RedisException("Stream read error");
                }
                return Task.FromResult(new[]
                {
                    new StreamEntry("1-0", new NameValueEntry[]
                    {
                        new NameValueEntry("Sequence", "1"),
                        new NameValueEntry("Name", "Test1"),
                        new NameValueEntry("Payload", "Payload1")
                    })
                });
            });

        // Act
        Task listenTask = Task.Run(() => _listener.ListenForPendingMessages(handler));

        await Task.Delay(1200); // LongWaitTime is 1000 ms
        _listener.StopListening();

        await listenTask;

        // Assert
        Assert.Equal(handlerCallCount, 1);
        _mockRedisConnectionManager.Verify(m => m.StreamReadAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>()), Times.AtLeast(2));
    }

    [Fact]
    public async Task ListenForPendingMessages_SubscribeAsyncFails_ShouldRetrySubscription()
    {
        // Arrange
        var handlerCalled = false;
        Func<List<MyStreamMessage>, Task<bool>> handler = async (messages) =>
        {
            await Task.Delay(1);
            handlerCalled = true;
            return true;
        };

        _mockSyncLock.Setup(l => l.AcquireLock()).ReturnsAsync(true);
        _mockSyncLock.Setup(l => l.RemainingLockTime).Returns(TimeSpan.FromSeconds(10));

        var subscribeAttempts = 0;
        _mockRedisConnectionManager.Setup(m => m.SubscribeAsync())
            .Returns(() =>
            {
                subscribeAttempts++;
                if (subscribeAttempts == 1)
                {
                    throw new RedisException("Subscribe failed");
                }
                return Task.CompletedTask;
            });

        _mockRedisConnectionManager.Setup(m => m.StreamReadAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>()))
            .ReturnsAsync(new[]
            {
                new StreamEntry("1-0", new NameValueEntry[]
                {
                    new NameValueEntry("Sequence", "1"),
                    new NameValueEntry("Name", "Test1"),
                    new NameValueEntry("Payload", "Payload1")
                })
            });

        // Act
        Task listenTask = Task.Run(() => _listener.ListenForPendingMessages(handler));

        await Task.Delay(1200);
        _listener.StopListening();

        await listenTask;

        // Assert
        Assert.True(handlerCalled);
        Assert.Equal(2, subscribeAttempts);
    }

    [Fact]
    public async Task ListenForPendingMessages_HandlerReturnsFalse_ShouldStopProcessing()
    {
        // Arrange
        var handlerCallCount = 0;
        Func<List<MyStreamMessage>, Task<bool>> handler = async (messages) =>
        {
            await Task.Delay(1);
            handlerCallCount++;
            return false; // Simulate a scenario where the handler wants to stop processing
        };

        _mockSyncLock.Setup(l => l.AcquireLock()).ReturnsAsync(true);
        _mockSyncLock.Setup(l => l.RemainingLockTime).Returns(TimeSpan.FromSeconds(10));

        _mockRedisConnectionManager.Setup(m => m.SubscribeAsync()).Returns(Task.CompletedTask);
        _mockRedisConnectionManager.Setup(m => m.StreamReadAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>()))
            .ReturnsAsync(new[]
            {
                new StreamEntry("1-0", new NameValueEntry[]
                {
                    new NameValueEntry("Sequence", "1"),
                    new NameValueEntry("Name", "Test1"),
                    new NameValueEntry("Payload", "Payload1")
                })
            });

        // Act
        Task listenTask = Task.Run(() => _listener.ListenForPendingMessages(handler));

        await Task.Delay(200);
        _listener.StopListening();

        await listenTask;

        // Assert
        Assert.Equal(1, handlerCallCount);
    }

    [Fact]
    public async Task ListenForPendingMessages_LockExtensionFails_ShouldReleaseLockAndReacquire()
    {
        // Arrange
        var handlerCalled = false;
        Func<List<MyStreamMessage>, Task<bool>> handler = async (messages) =>
        {
            await Task.Delay(1);
            handlerCalled = true;
            return true;
        };

        var lockAcquisitions = 0;
        _mockSyncLock.Setup(l => l.AcquireLock()).ReturnsAsync(() =>
        {
            lockAcquisitions++;
            return true;
        });
        _mockSyncLock.Setup(l => l.RemainingLockTime).Returns(TimeSpan.FromMilliseconds(50));
        _mockSyncLock.Setup(l => l.ExtendLock(It.IsAny<TimeSpan>())).ReturnsAsync(false);
        _mockSyncLock.Setup(l => l.ReleaseLock()).ReturnsAsync(true);

        _mockRedisConnectionManager.Setup(m => m.SubscribeAsync()).Returns(Task.CompletedTask);
        _mockRedisConnectionManager.Setup(m => m.StreamReadAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>()))
            .ReturnsAsync(new[]
            {
                new StreamEntry("1-0", new NameValueEntry[]
                {
                    new NameValueEntry("Sequence", "1"),
                    new NameValueEntry("Name", "Test1"),
                    new NameValueEntry("Payload", "Payload1")
                })
            });

        // Act
        Task listenTask = Task.Run(() => _listener.ListenForPendingMessages(handler));

        await Task.Delay(300);
        _listener.StopListening();

        await listenTask;

        // Assert
        Assert.True(handlerCalled);
        Assert.True(lockAcquisitions > 1);
        _mockSyncLock.Verify(l => l.ReleaseLock(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ListenForPendingMessages_RedisConnectionFailureWithReconnectFailure_ShouldStopListening()
    {
        // Arrange
        var handlerCalled = false;
        Func<List<MyStreamMessage>, Task<bool>> handler = async (messages) =>
        {
            await Task.Delay(1);
            handlerCalled = true;
            return true;
        };

        _mockSyncLock.Setup(l => l.AcquireLock()).ReturnsAsync(true);
        _mockSyncLock.Setup(l => l.RemainingLockTime).Returns(TimeSpan.FromSeconds(1));

        int subscribeCallCount = 0;
        _mockRedisConnectionManager.Setup(m => m.SubscribeAsync())
            .Returns(() => {
                subscribeCallCount++;
                if (subscribeCallCount == 1)
                {
                    throw new RedisTimeoutException("Connection timeout on SubscribeAsync", CommandStatus.Unknown);
                }
                return Task.FromResult(true);
            });

        int reconnectCallCount = 0;
        _mockRedisConnectionManager.Setup(m => m.Reconnect())
            .ReturnsAsync(() => {
                reconnectCallCount++;
                return reconnectCallCount > 1;
            });

        // Act
        Task listenTask = Task.Run(() => _listener.ListenForPendingMessages(handler));
        await Task.Delay(1200); // Allow some time for reconnection attempts, LongWaitTime is 1000 ms
        _listener.StopListening();

        // Assert
        Assert.False(handlerCalled);
        Assert.True(reconnectCallCount == 1);
        _mockRedisConnectionManager.Verify(m => m.Reconnect(), Times.AtLeastOnce);
    }

    public void Dispose()
    {
        _mockRedisConnectionManager.Reset();
        _mockSyncLock.Reset();
        _listener.Dispose();
    }
}