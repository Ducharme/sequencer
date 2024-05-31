using Xunit;
using Moq;
using StackExchange.Redis;

namespace RedisAccessLayer.Tests
{

    public class DistributedLockTests
    {
          private readonly Mock<IRedisConfigurationFetcher> _configFetcherMock;
        private readonly Mock<IDatabase> _databaseMock;
        private readonly Mock<IConnectionMultiplexer> _connectionMultiplexerMock;
        private readonly Mock<ISubscriber> _subscriberMock;
        private readonly NativeLock _distributedLock;
        private const string expectedLockValue = "test-lock-value";

        public DistributedLockTests()
        {
            // Create mock objects
            _configFetcherMock = new Mock<IRedisConfigurationFetcher>();
            _connectionMultiplexerMock = new Mock<IConnectionMultiplexer>();
            _databaseMock = new Mock<IDatabase>();
            _subscriberMock = new Mock<ISubscriber>();
            var connectionMultiplexerWrapperMock = new Mock<IConnectionMultiplexerWrapper>();
            var configurationOptionsWrapper = new Mock<IConfigurationOptionsWrapper>();
            
            // Set up behavior
            const string clientName = "test-client";
            //var lockKey = "sequencer-master";
            var lockValue = clientName + "_" + Guid.NewGuid().ToString();
            var lockExpiry = TimeSpan.FromSeconds(1);

            _configFetcherMock.SetupGet(cf => cf.ClientName).Returns(clientName);
            _configFetcherMock.SetupGet(cf => cf.OptionsWrapper).Returns(configurationOptionsWrapper.Object);
            connectionMultiplexerWrapperMock.Setup(wrapper => wrapper.Connect(configurationOptionsWrapper.Object)).Returns(_connectionMultiplexerMock.Object);
            _connectionMultiplexerMock.Setup(m => m.GetDatabase(It.IsAny<int>(), null)).Returns(_databaseMock.Object);
            _connectionMultiplexerMock.Setup(m => m.GetSubscriber(null)).Returns(_subscriberMock.Object);
            //_connectionMultiplexerMock.Setup(m => m.GetDatabase(It.IsAny<int>(), null).StringSetAsync(lockKey, lockValue, lockExpiry, It.IsAny<When>(), It.IsAny<CommandFlags>())).ReturnsAsync(true);

            var connectionManager = new RedisConnectionManager(_configFetcherMock.Object, connectionMultiplexerWrapperMock.Object);
            _distributedLock = new NativeLock(connectionManager);
        }

        internal void Dispose()
        {
            _distributedLock.Dispose();
        }

        [Fact]
        public async void IsLockAcquired_WhenLockNotAcquired_ReturnsFalse()
        {
            // Arrange
            await _distributedLock.AcquireLock();
            await _distributedLock.ReleaseLock();

            // Act
            bool isLockAcquired = _distributedLock.IsLockAcquired;

            // Assert
            Assert.False(isLockAcquired);
        }

        [Fact]
        public async void IsLockAcquired_WhenLockExpired_ReturnsFalse()
        {
            // Arrange
            await _distributedLock.AcquireLock();
            var ms = LockBase.DefaultLockExpiry.TotalMilliseconds / 2;
            var buffer = TimeSpan.FromMilliseconds(ms);
            var wait = LockBase.DefaultLockExpiry.Add(buffer);
            await Task.Delay(wait);

            // Act
            bool isLockAcquired = _distributedLock.IsLockAcquired;

            // Assert
            Assert.False(isLockAcquired);
        }

        [Fact]
        public async void IsLockAcquired_WhenLockNotExpired_ReturnsTrue()
        {
            // Arrange
            _databaseMock.Setup(db => db.LockTakeAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan>(), It.IsAny<CommandFlags>())).ReturnsAsync(true);
            await _distributedLock.AcquireLock();


            var ms = LockBase.DefaultLockExpiry.TotalMilliseconds / 2;
            await Task.Delay(TimeSpan.FromMilliseconds(ms));

            // Act
            bool isLockAcquired = _distributedLock.IsLockAcquired;

            // Assert
            Assert.True(isLockAcquired);
        }

        [Fact]
        public async void AcquireLock_WhenLockAcquired_ReturnsTrue()
        {
            // Arrange
            _databaseMock.Setup(db => db.LockTakeAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan>(), It.IsAny<CommandFlags>())).ReturnsAsync(true);

            // Act
            bool acquiredLock = await _distributedLock.AcquireLock();

            // Assert
            Assert.True(acquiredLock);
        }

        [Fact]
        public async void AcquireLock_WhenLockNotAcquired_ReturnsFalse()
        {
            // Arrange
            _databaseMock.Setup(db => db.LockTakeAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan>(), It.IsAny<CommandFlags>())).ReturnsAsync(false);
            
            // Act
            bool acquiredLock = await _distributedLock.AcquireLock();

            // Assert
            Assert.False(acquiredLock);
        }

        [Fact]
        public async void ExtendLock_WhenLockExtended_ReturnsTrue()
        {
            // Arrange
            _databaseMock.Setup(db => db.LockExtendAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan>(), It.IsAny<CommandFlags>())).ReturnsAsync(true);

            // Act
            bool extendedLock = await _distributedLock.ExtendLock(LockBase.DefaultLockExpiry);

            // Assert
            Assert.True(extendedLock);
        }

        [Fact]
        public async void ExtendLock_WhenLockNotExtended_ReturnsFalse()
        {
            // Arrange
            _databaseMock.Setup(db => db.LockExtendAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan>(), It.IsAny<CommandFlags>())).ReturnsAsync(false);

            // Act
            bool extendedLock = await _distributedLock.ExtendLock(LockBase.DefaultLockExpiry);

            // Assert
            Assert.False(extendedLock);
        }

        [Fact]
        public async void GetLockValue_ReturnsLockValue()
        {
            // Arrange
            _databaseMock.Setup(db => db.LockQueryAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>())).ReturnsAsync(expectedLockValue);

            // Act
            string? lockValue = await _distributedLock.GetLockValue();

            // Assert
            Assert.Equal(expectedLockValue, lockValue);
        }

        [Fact]
        public async void GetRemainingLockTime_ReturnsRemainingTime()
        {
            // Arrange
            _databaseMock.Setup(db => db.LockTakeAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan>(), It.IsAny<CommandFlags>())).ReturnsAsync(true);
            var ms = LockBase.DefaultLockExpiry.TotalMilliseconds / 2;
            await _distributedLock.AcquireLock();
            await Task.Delay(TimeSpan.FromMilliseconds(ms));

            // Act
            var remainingLockTime = _distributedLock.GetRemainingLockTime();
            
            // Assert
            Assert.InRange(remainingLockTime.TotalMilliseconds, 0.0, NativeLock.DefaultLockExpiry.TotalMilliseconds);
        }

        [Fact]
        public async void ReleaseLock_ReleasesLock()
        {
            // Arrange
            await _distributedLock.AcquireLock();

            // Act
            await _distributedLock.ReleaseLock();

            // Assert
            Assert.False(_distributedLock.IsLockAcquired);
        }
    }
}
