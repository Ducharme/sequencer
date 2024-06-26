using Xunit;
using Moq;
using StackExchange.Redis;

namespace RedisAccessLayer.Tests
{

    public class BasicMockNativeLockTests : BasicMockTests
    {
        protected readonly ISyncLock _distributedLock;

        public BasicMockNativeLockTests()
            : base()
        {
            _distributedLock = new NativeLock(_connectionManager);
        }

        internal void Dispose()
        {
            (_distributedLock as IDisposable)?.Dispose();
        }

        [Fact]
        public async void AcquireLock_WhenLockAcquired_ReturnsTrue()
        {
            // Arrange
            _databaseMock.Setup(db => db.LockTakeAsync(_distributedLock.LockKey, _distributedLock.LockValue, _distributedLock.LockExpiry, It.IsAny<CommandFlags>())).ReturnsAsync(true);

            // Act
            bool acquired = await _distributedLock.AcquireLock();

            // Assert
            Assert.True(acquired);
        }

        [Fact]
        public async void AcquireLock_WhenLockNotAcquired_ReturnsFalse()
        {
            // Arrange
            _databaseMock.Setup(db => db.LockTakeAsync(_distributedLock.LockKey, _distributedLock.LockValue, _distributedLock.LockExpiry, It.IsAny<CommandFlags>())).ReturnsAsync(false);

            // Act
            var acquired = await _distributedLock.AcquireLock();
            bool isLockAcquired = _distributedLock.IsLockAcquired;

            // Assert
            Assert.False(acquired);
            Assert.False(isLockAcquired);
        }

        [Fact]
        public async void IsLockAcquired_WhenLockNotExpired_ReturnsTrue()
        {
            // Arrange
            _databaseMock.Setup(db => db.LockTakeAsync(_distributedLock.LockKey, _distributedLock.LockValue, _distributedLock.LockExpiry, It.IsAny<CommandFlags>())).ReturnsAsync(true);
            var ms = _distributedLock.LockExpiry.TotalMilliseconds / 2;

            // Act
            var acquired = await _distributedLock.AcquireLock();
            await Task.Delay(TimeSpan.FromMilliseconds(ms));
            bool isLockAcquired = _distributedLock.IsLockAcquired;

            // Assert
            Assert.True(acquired);
            Assert.True(isLockAcquired);
        }

        [Fact]
        public async void IsLockAcquired_WhenLockExpired_ReturnsFalse()
        {
            // Arrange
            _databaseMock.Setup(db => db.LockTakeAsync(_distributedLock.LockKey, _distributedLock.LockValue, _distributedLock.LockExpiry, It.IsAny<CommandFlags>())).ReturnsAsync(true);
            var ms = _distributedLock.LockExpiry.TotalMilliseconds / 2 * 3;
            var wait = TimeSpan.FromMilliseconds(ms);

            // Act
            var acquired = await _distributedLock.AcquireLock();
            await Task.Delay(wait);
            bool isLockAcquired = _distributedLock.IsLockAcquired;

            // Assert
            Assert.True(acquired);
            Assert.False(isLockAcquired);
        }

        [Fact]
        public async void ExtendLock_WhenLockExtended_ReturnsTrue()
        {
            // Arrange
            _databaseMock.Setup(db => db.LockTakeAsync(_distributedLock.LockKey, _distributedLock.LockValue, _distributedLock.LockExpiry, It.IsAny<CommandFlags>())).ReturnsAsync(true);
            _databaseMock.Setup(db => db.LockExtendAsync(_distributedLock.LockKey, _distributedLock.LockValue, _distributedLock.LockExpiry, It.IsAny<CommandFlags>())).ReturnsAsync(true);

            // Act
            var acquired = await _distributedLock.AcquireLock();
            bool extended = await _distributedLock.ExtendLock(_distributedLock.LockExpiry);

            // Assert
            Assert.True(acquired);
            Assert.True(extended);
        }

        [Fact]
        public async void ExtendLock_WhenLockNotExtended_ReturnsFalse()
        {
            // Arrange
            _databaseMock.Setup(db => db.LockTakeAsync(_distributedLock.LockKey, _distributedLock.LockValue, _distributedLock.LockExpiry, It.IsAny<CommandFlags>())).ReturnsAsync(true);
            _databaseMock.Setup(db => db.LockExtendAsync(_distributedLock.LockKey, _distributedLock.LockValue, _distributedLock.LockExpiry, It.IsAny<CommandFlags>())).ReturnsAsync(false);

            // Act
            var acquired = await _distributedLock.AcquireLock();
            bool extended = await _distributedLock.ExtendLock(_distributedLock.LockExpiry);

            // Assert
            Assert.True(acquired);
            Assert.False(extended);
        }

        [Fact]
        public async void GetLockValue_ReturnsLockValue()
        {
            // Arrange
            _databaseMock.Setup(db => db.LockTakeAsync(_distributedLock.LockKey, _distributedLock.LockValue, _distributedLock.LockExpiry, It.IsAny<CommandFlags>())).ReturnsAsync(true);
            _databaseMock.Setup(db => db.LockQueryAsync(_distributedLock.LockKey, It.IsAny<CommandFlags>())).ReturnsAsync(_distributedLock.LockValue);

            // Act
            var acquired = await _distributedLock.AcquireLock();
            string? lockValue = await _distributedLock.GetLockValue();

            // Assert
            Assert.True(acquired);
            Assert.Equal(_distributedLock.LockValue, lockValue);
        }

        [Fact]
        public async void GetRemainingLockTime_ReturnsRemainingTime()
        {
            // Arrange
            _databaseMock.Setup(db => db.LockTakeAsync(_distributedLock.LockKey, _distributedLock.LockValue, _distributedLock.LockExpiry, It.IsAny<CommandFlags>())).ReturnsAsync(true);
            var ms = _distributedLock.LockExpiry.TotalMilliseconds / 2;

            // Act
            var acquired = await _distributedLock.AcquireLock();
            await Task.Delay(TimeSpan.FromMilliseconds(ms));
            var remainingLockTime = _distributedLock.RemainingLockTime;
            
            // Assert
            Assert.True(acquired);
            Assert.InRange(remainingLockTime.TotalMilliseconds, 0.0, _distributedLock.LockExpiry.TotalMilliseconds);
        }

        [Fact]
        public async void ReleaseLock_ReleasesLock()
        {
            // Arrange
            _databaseMock.Setup(db => db.LockTakeAsync(_distributedLock.LockKey, _distributedLock.LockValue, _distributedLock.LockExpiry, It.IsAny<CommandFlags>())).ReturnsAsync(true);
            _databaseMock.Setup(db => db.LockReleaseAsync(_distributedLock.LockKey, _distributedLock.LockValue, It.IsAny<CommandFlags>())).ReturnsAsync(true);

            // Act
            var acquired = await _distributedLock.AcquireLock();
            var released = await _distributedLock.ReleaseLock();

            // Assert
            Assert.True(acquired);
            Assert.True(released);
            Assert.False(_distributedLock.IsLockAcquired);
        }
    }
}
