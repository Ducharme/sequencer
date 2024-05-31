
# Knowledge Base

## AWS Serverless Redis

`
StackExchange.Redis.RedisCommandException: Command cannot be issued to a replica
 -> CommandFlags set to DemandMaster or PreferMaster

StackExchange.Redis.RedisConnectionException: No connection (requires writable - not eligible for replica) is active/available to service this operation: RPOPLPUSH

// NOTE: Conditions are not supported in AWS Serverless Redis (WATCH and UNWATCH are not available)
// RedisCommandException: This operation has been disabled in the command-map and cannot be used: WATCH
// LockExtendAsync -> GetLockExtendTransaction -> AddCondition
LockTake and LockExtend and LockRelease are not available

REDIS_USE_COMMAND_MAP=true to disable WATCH and UNWATCH
// NOTE: WATCH and UNWATCH are unavailable for serverless caches according to
// https://docs.aws.amazon.com/AmazonElastiCache/latest/red-ug/SupportedCommands.html
// Leads to StackExchange.Redis.RedisServerException: ERR unknown command 'unwatch', with args beginning with:

// NOTE: To avoid RedisCommandException -> Multi-key operations must involve a single slot; keys can use 'hash tags'
// Needed with AWS Serverless Redis
var channelPrefix = EnvVarReader.GetString("REDIS_CHANNEL_PREFIX", EnvVarReader.NotFound);
          
// NOTE: Although the channel prefix is being set, AWS Serverless Redis does not seem to support it so ChannelPrefixName is prefixed
// Error -> StackExchange.Redis.RedisCommandException: Multi-key operations must involve a single slot; keys can use 'hash tags' to help this,
// i.e. '{/users/12345}/account' and '{/users/12345}/contacts' will always be in the same slot
`

[notify-keyspace-events-> Keyspace events are currently not supported on serverless caches.](https://docs.aws.amazon.com/AmazonElastiCache/latest/red-ug/RedisConfiguration.html)


## bdg

```
StackExchange.Redis.RedisTimeoutException: Timeout awaiting response (outbound=0KiB, inbound=0KiB, 5636ms elapsed, timeout is 5000ms), command=EVAL, next: EVAL, inst: 0, qu: 0, qs: 1, aw: False, bw: Inactive, rs: ReadAsync, ws: Idle, in: 0, in-pipe: 0, out-pipe: 0, last-in: 0, cur-in: 0, sync-ops: 0, async-ops: 170, serverEndpoint: 172.17.0.3:6379, conn-sec: 25.16, aoc: 0, mc: 1/1/0, mgr: 10 of 10 available, clientName: seq-client-claude-virtualbox-25923, IOCP: (Busy=0,Free=1000,Min=1,Max=1000), WORKER: (Busy=1,Free=32766,Min=4,Max=32767), POOL: (Threads=5,QueuedItems=0,CompletedItems=1182,Timers=1), v: 2.6.122.38350 (Please take a look at this article for some common client-side issues that can cause timeouts: https://stackexchange.github.io/StackExchange.Redis/Timeouts)
   at StackExchange.Redis.RedisDatabase.ScriptEvaluateAsync(String script, RedisKey[] keys, RedisValue[] values, CommandFlags flags) in /_/src/StackExchange.Redis/RedisDatabase.cs:line 1552
   at RedisAccessLayer.RedisConnectionManager.ListRightPopLeftPushListSetByIndexInTransactionAsync(RedisKey source, RedisKey destination, Int64 val) in /home/claude/Documents/HKJC/sequencer/DataAccessLayers/RedisAccessLayer/Connection/RedisConnectionManager.cs:line 202

Redis Internal Error: Pipelines.Sockets.Unofficial.ConnectionAbortedException: The connection was aborted
   at System.IO.Pipelines.PipeCompletion.ThrowLatchedException()
   at System.IO.Pipelines.Pipe.GetReadAsyncResult()
   at StackExchange.Redis.PhysicalConnection.ReadFromPipe()


Redis Connection Failed: StackExchange.Redis.RedisConnectionException: InternalFailure (ReadSocketError/OperationAborted, last-recv: 42) on 172.17.0.3:6379/Subscription, Idle/Faulted, last: SUBSCRIBE, origin: ReadFromPipe, outstanding: 0, last-read: 0s ago, last-write: 13s ago, keep-alive: 60s, state: ConnectedEstablished, mgr: 9 of 10 available, in: 0, in-pipe: 0, out-pipe: 0, last-heartbeat: 0s ago, last-mbeat: 0s ago, global: 0s ago, v: 2.6.122.38350
 ---> Pipelines.Sockets.Unofficial.ConnectionAbortedException: The connection was aborted
   at System.IO.Pipelines.PipeCompletion.ThrowLatchedException()
   at System.IO.Pipelines.Pipe.GetReadAsyncResult()
   at StackExchange.Redis.PhysicalConnection.ReadFromPipe()
   --- End of inner exception stack trace ---
2024-05-26 22:54:15,647 [ERROR] RedisAccessLayer.RedisConnectionManager: Redis Internal Error: Pipelines.Sockets.Unofficial.ConnectionAbortedException: The connection was aborted
   at System.IO.Pipelines.PipeCompletion.ThrowLatchedException()
   at System.IO.Pipelines.Pipe.GetReadAsyncResult()
   at StackExchange.Redis.PhysicalConnection.ReadFromPipe()
2024-05-26 22:54:15,648 [ERROR] RedisAccessLayer.RedisConnectionManager: Redis Internal Error: System.InvalidOperationException: Writing is not allowed after writer was completed.
   at System.IO.Pipelines.ThrowHelper.ThrowInvalidOperationException_NoWritingAllowed()
   at StackExchange.Redis.PhysicalConnection.WriteHeader(RedisCommand command, Int32 arguments, CommandBytes commandBytes) in /_/src/StackExchange.Redis/PhysicalConnection.cs:line 809
   at StackExchange.Redis.Message.CommandMessage.WriteImpl(PhysicalConnection physical) in /_/src/StackExchange.Redis/Message.cs:line 1395
   at StackExchange.Redis.Message.WriteTo(PhysicalConnection physical) in /_/src/StackExchange.Redis/Message.cs:line 699
```

## DOTNET

```
>> E: Unable to locate package dotnet-sdk-8.0.203-1
>> E: Couldn't find any package by glob 'dotnet-sdk-8.0.203-1'
>> E: Couldn't find any package by regex 'dotnet-sdk-8.0.203-1'
```
or
```
>> You must install or update .NET to run this application.
>> No frameworks were found.
```
Refer to DOTNET section in INSTALL.md
