
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
