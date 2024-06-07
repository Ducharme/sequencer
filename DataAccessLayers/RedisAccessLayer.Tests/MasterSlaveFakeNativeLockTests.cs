using Xunit;
using Moq;
using StackExchange.Redis;

namespace RedisAccessLayer.Tests
{
    public class MasterSlaveFakeNativeLockTests : MasterSlaveFakeBaseTests
    {
        protected readonly ISyncLock _distributedLock1;
        protected readonly ISyncLock _distributedLock2;
        protected readonly ISyncLock _distributedLock3;
        protected readonly RedisFakeDatabase _redis = new ();

        public MasterSlaveFakeNativeLockTests()
            : base()
        {
            _distributedLock1 = new NativeLock(_connectionManager);
            _distributedLock2 = new NativeLock(_connectionManager);
            _distributedLock3 = new NativeLock(_connectionManager);
        }

        internal protected void Dispose()
        {
            (_distributedLock1 as IDisposable)?.Dispose();
            (_distributedLock2 as IDisposable)?.Dispose();
            (_distributedLock3 as IDisposable)?.Dispose();
        }

        [Fact]
        public async void AcquireLock_OnlyOneMaster_ExtendNearExpiry()
        {
            // Arrange
            _databaseMock.Setup(db => db.LockTakeAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan>(), It.IsAny<CommandFlags>()))
                .Returns((RedisKey key, RedisValue value, TimeSpan expiry, CommandFlags flags) => _redis.LockTakeAsync(key, value, expiry));
            _databaseMock.Setup(db => db.LockExtendAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan>(), It.IsAny<CommandFlags>()))
                .Returns((RedisKey key, RedisValue value, TimeSpan expiry, CommandFlags flags) => _redis.LockExtendAsync(key, value, expiry));
            _databaseMock.Setup(db => db.LockQueryAsync(_distributedLock1.LockKey, It.IsAny<CommandFlags>()))
                .Returns((RedisKey key, CommandFlags flags) => _redis.LockQueryAsync(key));
            _databaseMock.Setup(db => db.LockReleaseAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
                .Returns((RedisKey key, RedisValue value, CommandFlags flags) => _redis.LockReleaseAsync(key, value));

            // Act
            List<Task<bool>> runTasks = [];
            int[] releaseAtIndices = [];
            runTasks.Add(Task.Run(() => RunLoop(_distributedLock1, 0, releaseAtIndices)));
            runTasks.Add(Task.Run(() => RunLoop(_distributedLock2, 1, releaseAtIndices)));
            runTasks.Add(Task.Run(() => RunLoop(_distributedLock3, 2, releaseAtIndices)));
            var results = await Task.WhenAll(runTasks);

            // Assert
            var sum = results.Select(r => r == false ? 0 : 1).Sum();
            Assert.Equal(1, sum);
            _databaseMock.Verify(db => db.LockExtendAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan>(), It.IsAny<CommandFlags>()), Times.Exactly(1));
        }

        [Fact]
        public async void AcquireLock_OnlyOneMaster_ReleaseAtIndex()
        {
            // Arrange
            _databaseMock.Setup(db => db.LockTakeAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan>(), It.IsAny<CommandFlags>()))
                .Returns((RedisKey key, RedisValue value, TimeSpan expiry, CommandFlags flags) => _redis.LockTakeAsync(key, value, expiry));
            _databaseMock.Setup(db => db.LockExtendAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan>(), It.IsAny<CommandFlags>()))
                .Returns((RedisKey key, RedisValue value, TimeSpan expiry, CommandFlags flags) => _redis.LockExtendAsync(key, value, expiry));
            _databaseMock.Setup(db => db.LockQueryAsync(_distributedLock1.LockKey, It.IsAny<CommandFlags>()))
                .Returns((RedisKey key, CommandFlags flags) => _redis.LockQueryAsync(key));
            _databaseMock.Setup(db => db.LockReleaseAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
                .Returns((RedisKey key, RedisValue value, CommandFlags flags) => _redis.LockReleaseAsync(key, value));

            // Act
            int[] releaseAtIndices = [4, 8, 12];
            List<Task<bool>> runTasks = [];
            runTasks.Add(Task.Run(() => RunLoop(_distributedLock1, 0, releaseAtIndices)));
            runTasks.Add(Task.Run(() => RunLoop(_distributedLock2, 1, releaseAtIndices)));
            runTasks.Add(Task.Run(() => RunLoop(_distributedLock3, 2, releaseAtIndices)));
            var results = await Task.WhenAll(runTasks);

            // Assert
            var sum = results.Select(r => r == false ? 0 : 1).Sum();
            Assert.Equal(1, sum);
            //_databaseMock.Verify(db => db.LockReleaseAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()), Times.Between(3, 4, Moq.Range.Inclusive));
        }
    }
}
