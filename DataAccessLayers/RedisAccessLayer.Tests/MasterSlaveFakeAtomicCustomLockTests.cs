using Xunit;
using Moq;
using StackExchange.Redis;

namespace RedisAccessLayer.Tests
{
    public class MasterSlaveFakeAtomicCustomLockTests : MasterSlaveFakeBaseTests
    {
        protected readonly ISyncLock _distributedLock1;
        protected readonly ISyncLock _distributedLock2;
        protected readonly ISyncLock _distributedLock3;
        protected readonly RedisFakeDatabase _redis = new ();

        public MasterSlaveFakeAtomicCustomLockTests()
            : base()
        {
            _distributedLock1 = new AtomicCustomLock(_connectionManager);
            _distributedLock2 = new AtomicCustomLock(_connectionManager);
            _distributedLock3 = new AtomicCustomLock(_connectionManager);
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
            _databaseMock.Setup(db => db.ScriptEvaluateAsync(AtomicCustomLock.AcquireScript, It.IsAny<RedisKey[]>(), It.IsAny<RedisValue[]>(), It.IsAny<CommandFlags>()))
                .Returns((string script, RedisKey[] keys, RedisValue[] values, CommandFlags flags) => _redis.ScriptEvaluateAsync(script, keys, values, flags));
            _databaseMock.Setup(db => db.ScriptEvaluateAsync(AtomicCustomLock.ExtendScript, It.IsAny<RedisKey[]>(), It.IsAny<RedisValue[]>(), It.IsAny<CommandFlags>()))
                .Returns((string script, RedisKey[] keys, RedisValue[] values, CommandFlags flags) => _redis.ScriptEvaluateAsync(script, keys, values, flags));
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
            _databaseMock.Setup(db => db.ScriptEvaluateAsync(AtomicCustomLock.AcquireScript, It.IsAny<RedisKey[]>(), It.IsAny<RedisValue[]>(), It.IsAny<CommandFlags>()))
                .Returns((string script, RedisKey[] keys, RedisValue[] values, CommandFlags flags) => _redis.ScriptEvaluateAsync(script, keys, values, flags));
            _databaseMock.Setup(db => db.ScriptEvaluateAsync(AtomicCustomLock.ExtendScript, It.IsAny<RedisKey[]>(), It.IsAny<RedisValue[]>(), It.IsAny<CommandFlags>()))
                .Returns((string script, RedisKey[] keys, RedisValue[] values, CommandFlags flags) => _redis.ScriptEvaluateAsync(script, keys, values, flags));
            _databaseMock.Setup(db => db.ScriptEvaluateAsync(AtomicCustomLock.ReleaseScript, It.IsAny<RedisKey[]>(), It.IsAny<RedisValue[]>(), It.IsAny<CommandFlags>()))
                .Returns((string script, RedisKey[] keys, RedisValue[] values, CommandFlags flags) => _redis.ScriptEvaluateAsync(script, keys, values, flags));
            _databaseMock.Setup(db => db.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .Returns((RedisKey key, CommandFlags flags) => _redis.StringGetAsync(key, flags));

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
