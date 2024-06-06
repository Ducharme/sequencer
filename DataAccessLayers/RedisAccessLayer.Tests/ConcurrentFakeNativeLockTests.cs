using Xunit;
using Moq;
using StackExchange.Redis;
using NuGet.Frameworks;

namespace RedisAccessLayer.Tests
{
    public class ConcurrentFakeNativeLockTests : BasicMockTests
    {
        private readonly ISyncLock _distributedLock1;
        private readonly ISyncLock _distributedLock2;
        private readonly RedisFakeDatabase _redis = new ();

        public ConcurrentFakeNativeLockTests()
            : base()
        {
            _distributedLock1 = new NativeLock(_connectionManager);
            _distributedLock2 = new NativeLock(_connectionManager);
        }

        internal void Dispose()
        {
            (_distributedLock1 as IDisposable)?.Dispose();
        }

        [Fact]
        public async void AcquireLock_WhenLockAlreadyAcquired_SecondLockAcquiredReturnsFalse()
        {
            // Arrange
            _databaseMock.Setup(db => db.LockTakeAsync(_distributedLock1.LockKey, _distributedLock1.LockValue, _distributedLock1.LockExpiry, It.IsAny<CommandFlags>()))
                .Returns((RedisKey key, RedisValue value, TimeSpan expiry, CommandFlags flags) => _redis.LockTakeAsync(key, value, expiry));
            _databaseMock.Setup(db => db.LockQueryAsync(_distributedLock1.LockKey, It.IsAny<CommandFlags>()))
                .Returns((RedisKey key, CommandFlags flags) => _redis.LockQueryAsync(key));

            _databaseMock.Setup(db => db.LockTakeAsync(_distributedLock2.LockKey, _distributedLock2.LockValue, _distributedLock2.LockExpiry, It.IsAny<CommandFlags>()))
                .Returns((RedisKey key, RedisValue value, TimeSpan expiry, CommandFlags flags) => _redis.LockTakeAsync(key, value, expiry));
            _databaseMock.Setup(db => db.LockQueryAsync(_distributedLock2.LockKey, It.IsAny<CommandFlags>()))
                .Returns((RedisKey key, CommandFlags flags) => _redis.LockQueryAsync(key));

            // Act
            bool acquired1 = await _distributedLock1.AcquireLock();
            var rt1 = _distributedLock1.RemainingLockTime.TotalMilliseconds;
            bool isLockAcquired1 = _distributedLock1.IsLockAcquired;
            var lockValue1 = await _distributedLock1.GetLockValue();

            bool acquired2 = await _distributedLock2.AcquireLock();
            var rt2 = _distributedLock2.RemainingLockTime.TotalMilliseconds;
            bool isLockAcquired2 = _distributedLock2.IsLockAcquired;
            var lockValue2 = await _distributedLock2.GetLockValue();

            // Assert
            Assert.True(acquired1);
            Assert.True(isLockAcquired1);
            Assert.InRange(rt1, 0.0, _distributedLock1.LockExpiry.TotalMilliseconds);
            Assert.Equal(lockValue1, _distributedLock1.LockValue);

            Assert.False(acquired2);
            Assert.False(isLockAcquired2);
            Assert.NotEqual(lockValue2, _distributedLock2.LockValue);
            Assert.Equal(lockValue2, _distributedLock1.LockValue);
        }

        [Fact]
        public async void AcquireLock_WhenFirstLockExpired_SecondLockAcquiredReturnsTrue()
        {
            // Arrange
            _databaseMock.Setup(db => db.LockTakeAsync(_distributedLock1.LockKey, _distributedLock1.LockValue, _distributedLock1.LockExpiry, It.IsAny<CommandFlags>()))
                .Returns((RedisKey key, RedisValue value, TimeSpan expiry, CommandFlags flags) => _redis.LockTakeAsync(key, value, expiry));
            _databaseMock.Setup(db => db.LockTakeAsync(_distributedLock2.LockKey, _distributedLock2.LockValue, _distributedLock2.LockExpiry, It.IsAny<CommandFlags>()))
                .Returns((RedisKey key, RedisValue value, TimeSpan expiry, CommandFlags flags) => _redis.LockTakeAsync(key, value, expiry));
            var ms = _distributedLock1.LockExpiry.TotalMilliseconds / 2 * 3;

            // Act
            bool acquired1 = await _distributedLock1.AcquireLock();
            await Task.Delay(TimeSpan.FromMilliseconds(ms));
            bool acquired2 = await _distributedLock2.AcquireLock();
            var rt1 = _distributedLock1.RemainingLockTime.TotalMilliseconds;
            bool isLockAcquired1 = _distributedLock1.IsLockAcquired;
            var rt2 = _distributedLock2.RemainingLockTime.TotalMilliseconds;
            bool isLockAcquired2 = _distributedLock2.IsLockAcquired;

            // Assert
            Assert.True(acquired1);
            Assert.False(isLockAcquired1);
            Assert.True(rt1 < 0.0);
            Assert.True(acquired2);
            Assert.True(isLockAcquired2);
            Assert.InRange(rt2, 0.0, _distributedLock1.LockExpiry.TotalMilliseconds);
        }

        [Fact]
        public async void ExtendLock_WhenFirstLockExtended_SecondLockAcquiredReturnsFalse()
        {
            // Arrange
            _databaseMock.Setup(db => db.LockTakeAsync(_distributedLock1.LockKey, _distributedLock1.LockValue, _distributedLock1.LockExpiry, It.IsAny<CommandFlags>()))
                .Returns((RedisKey key, RedisValue value, TimeSpan expiry, CommandFlags flags) => _redis.LockTakeAsync(key, value, expiry));
            _databaseMock.Setup(db => db.LockExtendAsync(_distributedLock1.LockKey, _distributedLock1.LockValue, _distributedLock1.LockExpiry, It.IsAny<CommandFlags>()))
                .Returns((RedisKey key, RedisValue value, TimeSpan expiry, CommandFlags flags) => _redis.LockExtendAsync(key, value, expiry));

            _databaseMock.Setup(db => db.LockTakeAsync(_distributedLock2.LockKey, _distributedLock2.LockValue, _distributedLock2.LockExpiry, It.IsAny<CommandFlags>()))
                .Returns((RedisKey key, RedisValue value, TimeSpan expiry, CommandFlags flags) => _redis.LockTakeAsync(key, value, expiry));
            var ms = _distributedLock1.LockExpiry.TotalMilliseconds / 2;

            // Act
            bool acquired1 = await _distributedLock1.AcquireLock();
            await Task.Delay(TimeSpan.FromMilliseconds(ms)); // From 1000 ms to 500 ms
            bool extended1 = await _distributedLock1.ExtendLock(_distributedLock1.LockExpiry);
            await Task.Delay(TimeSpan.FromMilliseconds(ms)); // From 1000 ms to 500 ms, without extend it would be 0 ms
            bool extended2 = await _distributedLock1.ExtendLock(_distributedLock1.LockExpiry);
            await Task.Delay(TimeSpan.FromMilliseconds(ms)); // From 1000 ms to 500 ms, without extend it would be -500 ms
            bool acquired2 = await _distributedLock2.AcquireLock();
            var rt1 = _distributedLock1.RemainingLockTime.TotalMilliseconds;

            // Assert
            Assert.True(acquired1);
            Assert.True(extended1);
            Assert.True(extended2);
            Assert.InRange(rt1, 0.0, _distributedLock1.LockExpiry.TotalMilliseconds);
            Assert.False(acquired2);
        }

        [Fact]
        public async void AcquireLock_WhenMultipleLockAcquired_EnsureContentionAllowsOnlyOne()
        {
            // Arrange
            const int maxNewLocks = 10;
            _databaseMock.Setup(db => db.LockTakeAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan>(), It.IsAny<CommandFlags>()))
                .Returns((RedisKey key, RedisValue value, TimeSpan expiry, CommandFlags flags) => _redis.LockTakeAsync(key, value, expiry));


            List<Task<NativeLock>> createTasks = [];
            for (int i = 0; i < maxNewLocks; i++)
            {
                createTasks.Add(Task.Run(() => new NativeLock(_connectionManager)));
            }
            var lcks = await Task.WhenAll(createTasks);

            List<Task<bool>> acquireTasks = new();
            acquireTasks.Add(Task.Run(() => _distributedLock1.AcquireLock()));
            acquireTasks.Add(Task.Run(() => _distributedLock2.AcquireLock()));
            for (int i = 0; i < maxNewLocks; i++)
            {
                var a = i;
                acquireTasks.Add(Task.Run(() => lcks[a].AcquireLock()));
            }

            // Act
            var results = await Task.WhenAll(acquireTasks);
            var sum = results.Select(r => r == false ? 0 : 1).Sum();

            // Assert
            Assert.Equal(1, sum);
            Assert.Equal(12, results.Count());
        }
    }
}
