using System.Diagnostics;
using StackExchange.Redis;

namespace RedisAccessLayer.Tests
{
    public class RedisFakeDatabase : IRedisFakeDatabase
    {
        private readonly Dictionary<RedisKey, Tuple<RedisValue, DateTime>> _stringValues = [];
        private readonly Dictionary<RedisKey, List<RedisValue>> _listValues = [];
        private readonly Dictionary<RedisKey, StreamEntry[]> _streamValues = [];
        private readonly Dictionary<RedisKey, Tuple<RedisValue, DateTime>> _lockValues = [];

        public Task SubscribeAsync(RedisChannel channel, Action<RedisChannel, RedisValue> handler, CommandFlags flags = CommandFlags.None)
        {
            // Mock implementation for subscription
            return Task.CompletedTask;
        }

        public Task<TimeSpan> PingAsync(CommandFlags flags = CommandFlags.None)
        {
            // Mock implementation for ping
            return Task.FromResult(TimeSpan.FromMilliseconds(1));
        }

        public Task<StreamEntry[]> StreamReadAsync(RedisKey key, RedisValue position, int? count = null, CommandFlags flags = CommandFlags.None)
        {
            // Mock implementation for stream read
            if (_streamValues.TryGetValue(key, out var entries))
            {
                return Task.FromResult(entries);
            }
            return Task.FromResult(Array.Empty<StreamEntry>());
        }

        public Task<RedisValue> ListGetByIndexAsync(RedisKey key, long index, CommandFlags flags = CommandFlags.None)
        {
            // Mock implementation for list get by index
            if (_listValues.TryGetValue(key, out var list) && index >= 0 && index < list.Count)
            {
                return Task.FromResult(list[(int)index]);
            }
            return Task.FromResult(RedisValue.Null);
        }

        public Task<RedisValue[]> ListRangeAsync(RedisKey key, long start = 0, long stop = -1, CommandFlags flags = CommandFlags.None)
        {
            // Mock implementation for list range
            if (_listValues.TryGetValue(key, out var list))
            {
                var startIndex = (int)Math.Max(start, 0);
                var stopIndex = (int)(stop == -1 ? list.Count - 1 : Math.Min(stop, list.Count - 1));
                var range = list.Skip(startIndex).Take(stopIndex - startIndex + 1).ToArray();
                return Task.FromResult(range);
            }
            return Task.FromResult(Array.Empty<RedisValue>());
        }

        public Task<bool> KeyDeleteAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            // Mock implementation for key delete
            var deleted = _stringValues.Remove(key) || _listValues.Remove(key) || _streamValues.Remove(key);
            return Task.FromResult(deleted);
        }

        public Task<RedisResult> ScriptEvaluateAsync(string script, RedisKey[]? keys = null, RedisValue[]? values = null, CommandFlags flags = CommandFlags.None)
        {
            if (script == AtomicCustomLock.AcquireScript)
            {
                if (keys != null && keys.Length >= 1 && values != null && values.Length >= 3 && !values[0].IsNullOrEmpty && !values[2].IsNullOrEmpty)
                {
                    var lockKey = !string.IsNullOrEmpty(keys[0]) ? keys[0].ToString() : throw new ArgumentException("Lock key must be a string");
                    var lockValue = (string?)values[0] ?? throw new ArgumentException("Lock value must be a string");
                    var expiry = (int)values[1];
                    var requestId = (string?)values[2] ?? throw new ArgumentException("RequestId must be a string");

                    var ts = TimeSpan.FromMilliseconds(expiry);
                    var hasValue = _stringValues.TryGetValue(lockKey, out var tuple);
                    bool success;
                    if (hasValue)
                    {
                        if (tuple == null)
                        {
                            _stringValues[lockKey] = new Tuple<RedisValue, DateTime>(lockValue, DateTime.UtcNow + ts);
                            success = true;
                        }
                        else
                        {
                            var hasExpired = tuple.Item2 < DateTime.UtcNow;
                            if (hasExpired)
                            {
                                _stringValues[lockKey] = new Tuple<RedisValue, DateTime>(lockValue, DateTime.UtcNow + ts);
                                success = true;
                            }
                            else
                            {
                                if (tuple.Item1 == lockValue)
                                {
                                    _stringValues[lockKey] = new Tuple<RedisValue, DateTime>(lockValue, DateTime.UtcNow + ts);
                                    success = true;
                                }
                                else
                                {
                                    _stringValues.Remove(lockKey);
                                    success = false;
                                }
                            }
                        }
                    }
                    else
                    {
                        _stringValues[lockKey] = new Tuple<RedisValue, DateTime>(lockValue, DateTime.UtcNow + ts);
                        success = true;
                    }
                    return Task.FromResult(success ? RedisResult.Create(requestId, ResultType.SimpleString) : RedisResult.Create(RedisValue.Null));
                }
            }
            else if (script == AtomicCustomLock.ExtendScript)
            {
                if (keys != null && keys.Length >= 1 && values != null && values.Length >= 3 && !values[0].IsNullOrEmpty && !values[2].IsNullOrEmpty)
                {
                    var lockKey = !string.IsNullOrEmpty(keys[0]) ? keys[0].ToString() : throw new ArgumentException("Lock key must be a string");
                    var lockValue = (string?)values[0] ?? throw new ArgumentException("Lock value must be a string");
                    var expiry = (int)values[1];
                    var requestId = (string?)values[2] ?? throw new ArgumentException("RequestId must be a string");

                    var ts = TimeSpan.FromMilliseconds(expiry);
                    var hasValue = _stringValues.TryGetValue(lockKey, out var tuple);
                    bool success;
                    if (hasValue)
                    {
                        if (tuple == null)
                        {
                            _stringValues.Remove(lockKey);
                            success = false;
                        }
                        else
                        {
                            var hasExpired = tuple.Item2 < DateTime.UtcNow;
                            if (hasExpired)
                            {
                                _stringValues.Remove(lockKey);
                                success = false;
                            }
                            else
                            {
                                if (tuple.Item1 == lockValue)
                                {
                                    _stringValues[lockKey] = new Tuple<RedisValue, DateTime>(lockValue, DateTime.UtcNow + ts);
                                    success = true;
                                }
                                else
                                {
                                    success = false;
                                }
                            }
                        }
                    }
                    else
                    {
                        success = false;
                    }
                    return Task.FromResult(success ? RedisResult.Create(requestId, ResultType.SimpleString) : RedisResult.Create(RedisValue.Null));
                }
            }
            else if (script == AtomicCustomLock.ReleaseScript)
            {
                //Console.WriteLine($"ScriptEvaluateAsync: ExtendScript");
                if (keys != null && keys.Length >= 1 && values != null && values.Length >= 2 && !values[0].IsNullOrEmpty && !values[1].IsNullOrEmpty)
                {
                    var lockKey = !string.IsNullOrEmpty(keys[0]) ? keys[0].ToString() : throw new ArgumentException("Lock key must be a string");
                    var lockValue = (string?)values[0] ?? throw new ArgumentException("Lock value must be a string");
                    var requestId = (string?)values[1] ?? throw new ArgumentException("RequestId value must be a string");
                    
                    var hasValue = _stringValues.TryGetValue(lockKey, out var tuple);
                    bool success;
                    if (hasValue)
                    {
                        if (tuple == null)
                        {
                            _stringValues.Remove(lockKey);
                            success = false;
                        }
                        else
                        {
                            var hasExpired = tuple.Item2 < DateTime.UtcNow;
                            if (hasExpired)
                            {
                                _stringValues.Remove(lockKey);
                                success = false;
                            }
                            else
                            {
                                if (tuple.Item1 == lockValue)
                                {
                                    success = _stringValues.Remove(lockKey);;
                                }
                                else
                                {
                                    success = false;
                                }
                            }
                        }
                    }
                    else
                    {
                        success = false;
                    }
                    return Task.FromResult(success ? RedisResult.Create(requestId, ResultType.SimpleString) : RedisResult.Create(RedisValue.Null));
                }
            }
            return Task.FromResult(RedisResult.Create(RedisValue.Null));
        }

        public ITransaction CreateTransaction(object? asyncState = null)
        {
            // Mock implementation for creating a transaction
            return new RedisTransactionMock();
        }

        public Task<long> ListRemoveAsync(RedisKey key, RedisValue value, long count = 0, CommandFlags flags = CommandFlags.None)
        {
            // Mock implementation for list remove
            if (_listValues.TryGetValue(key, out var list))
            {
                var removedCount = list.RemoveAll(v => v == value);
                return Task.FromResult((long)removedCount);
            }
            return Task.FromResult(0L);
        }

        public Task<RedisValue> ListRightPopAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            // Mock implementation for list right pop
            if (_listValues.TryGetValue(key, out var list) && list.Count > 0)
            {
                var value = list[list.Count - 1];
                list.RemoveAt(list.Count - 1);
                return Task.FromResult(value);
            }
            return Task.FromResult(RedisValue.Null);
        }

        public Task<bool> KeyExistsAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            // Mock implementation for key exists
            return Task.FromResult(_stringValues.ContainsKey(key) || _listValues.ContainsKey(key) || _streamValues.ContainsKey(key));
        }

        public Task<StreamInfo> StreamInfoAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            throw new NotImplementedException();
            /*
            // Mock implementation for stream info
            if (_streamValues.TryGetValue(key, out var entries))
            {
                //return Task.FromResult(new StreamInfo(entries.Length, entries.FirstOrDefault()?.Id ?? StreamPosition.NewId, entries.LastOrDefault()?.Id ?? StreamPosition.NewId));
            }
            return Task.FromResult(StreamInfo.Empty);
            */
        }

        /*public Task<StreamInfo> StreamInfoAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            // Mock implementation for stream info
            if (_streamValues.TryGetValue(key, out var entries))
            {
                var streamInfo = new StreamInfo
                {
                    Length = entries.Length,
                    FirstEntry = entries.FirstOrDefault(),
                    LastEntry = entries.LastOrDefault()
                };
                var si = new StreamInfo(key);
                return Task.FromResult(streamInfo);
            }
            return Task.FromResult(StreamInfo.Empty);
        }*/

        public Task<bool> StringSetAsync(RedisKey key, RedisValue value, TimeSpan? expiry, When when, CommandFlags flags)
        {
            var hasValue = _stringValues.TryGetValue(key, out var tuple);
            if (hasValue)
            {
                if (when == When.Exists)
                {
                     _stringValues[key] = new Tuple<RedisValue, DateTime>(value, expiry.HasValue ? DateTime.UtcNow + expiry.Value : DateTime.MaxValue);
                     return Task.FromResult(true);
                }
                else
                {
                    return Task.FromResult(false);
                }
            }
            else
            {
                if (when == When.NotExists)
                {
                     _stringValues[key] = new Tuple<RedisValue, DateTime>(value, expiry.HasValue ? DateTime.UtcNow + expiry.Value : DateTime.MaxValue);
                     return Task.FromResult(true);
                }
                else
                {
                    return Task.FromResult(false);
                }
            }
        }

        public Task<RedisValue> StringGetAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            var hasValue = _stringValues.TryGetValue(key, out var tuple);
            if (hasValue)
            {
                if (tuple == null)
                {
                    _stringValues.Remove(key);
                }
                else
                {
                    var hasExpired = tuple.Item2 < DateTime.UtcNow;
                    if (hasExpired)
                    {
                        _stringValues.Remove(key);
                    }
                    else
                    {
                        return Task.FromResult(tuple.Item1);
                    }
                }
            }
            return Task.FromResult(RedisValue.Null);
        }

        public Task<bool> LockTakeAsync(RedisKey key, RedisValue value, TimeSpan expiry, CommandFlags flags = CommandFlags.None)
        {
            if (!_lockValues.ContainsKey(key))
            {
                _lockValues[key] = new Tuple<RedisValue, DateTime>(value, DateTime.UtcNow + expiry);
                return Task.FromResult(true);
            }
            else
            {
                var tuple = _lockValues[key];
                if (tuple.Item2 < DateTime.UtcNow) // Expired
                {
                    _lockValues[key] = new Tuple<RedisValue, DateTime>(value, DateTime.UtcNow + expiry);
                    return Task.FromResult(true);
                }
                else
                {
                    return Task.FromResult(false);
                }
            }
        }

        public Task<bool> LockExtendAsync(RedisKey key, RedisValue value, TimeSpan expiry, CommandFlags flags = CommandFlags.None)
        {
            if (_lockValues.TryGetValue(key, out var tuple) && tuple.Item1 == value)
            {
                if (tuple.Item2 < DateTime.UtcNow) // Expired
                {
                    _lockValues.Remove(key);
                    return Task.FromResult(false);
                }
                else
                {
                    _lockValues[key] = new Tuple<RedisValue, DateTime>(value, DateTime.UtcNow + expiry);
                    return Task.FromResult(true);
                }
            }
            return Task.FromResult(false);
        }

        public Task<RedisValue> LockQueryAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            if (_lockValues.TryGetValue(key, out var tuple))
            {
                //Console.WriteLine("LockQueryAsync: Key found");
                if (tuple.Item2 < DateTime.UtcNow) // Expired
                {
                    //Console.WriteLine("LockQueryAsync: Expired");
                    _lockValues.Remove(key);
                    return Task.FromResult(RedisValue.Null);
                }
                else
                {
                    //Console.WriteLine("LockQueryAsync: Valid");
                    return Task.FromResult(tuple.Item1);
                }
            }
            //Console.WriteLine("LockQueryAsync: Key not found");
            return Task.FromResult(RedisValue.Null);
        }

        public Task<bool> LockReleaseAsync(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None)
        {
            if (_lockValues.TryGetValue(key, out var tuple) && tuple.Item1 == value)
            {
                _lockValues.Remove(key);
                if (tuple.Item2 < DateTime.UtcNow) // Expired
                {
                    return Task.FromResult(false);
                }
                else
                {
                    return Task.FromResult(true);
                }
            }
            return Task.FromResult(false);
        }
    }
}