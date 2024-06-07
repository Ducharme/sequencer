using Xunit;
using Moq;
using StackExchange.Redis;

namespace RedisAccessLayer.Tests
{
    public class MasterSlaveFakeSimpleCustomLockTests : MasterSlaveFakeBaseTests
    {
        protected readonly ISyncLock _distributedLock1;
        protected readonly ISyncLock _distributedLock2;
        protected readonly ISyncLock _distributedLock3;
        protected readonly RedisFakeDatabase _redis = new ();

        public MasterSlaveFakeSimpleCustomLockTests()
            : base()
        {
            _distributedLock1 = new SimpleCustomLock(_connectionManager);
            _distributedLock2 = new SimpleCustomLock(_connectionManager);
            _distributedLock3 = new SimpleCustomLock(_connectionManager);
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
            _databaseMock.Setup(db => db.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
                .Returns((RedisKey key, RedisValue value, TimeSpan? expiry, When when, CommandFlags flags) => _redis.StringSetAsync(key, value, expiry, when, flags));
            _databaseMock.Setup(db => db.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .Returns((RedisKey key, CommandFlags flags) => _redis.StringGetAsync(key, flags));

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
        }

        [Fact]
        public async void AcquireLock_OnlyOneMaster_ReleaseAtIndex()
        {
            // Arrange
            _databaseMock.Setup(db => db.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
                .Returns((RedisKey key, RedisValue value, TimeSpan? expiry, When when, CommandFlags flags) => _redis.StringSetAsync(key, value, expiry, when, flags));
            _databaseMock.Setup(db => db.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .Returns((RedisKey key, CommandFlags flags) => _redis.StringGetAsync(key, flags));
            _databaseMock.Setup(db => db.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .Returns((RedisKey key, CommandFlags flags) => _redis.KeyDeleteAsync(key, flags));

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
        }
    }
}
