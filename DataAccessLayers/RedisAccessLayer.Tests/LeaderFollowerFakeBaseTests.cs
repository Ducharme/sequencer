using Xunit;
using Moq;

namespace RedisAccessLayer.Tests
{
    public abstract class LeaderFollowerFakeBaseTests : BasicMockTests
    {
        protected static string GetTime()
        {
            return DateTime.UtcNow.ToString("HH:mm:ss.fff");
        }

        protected const int BufferTime = 100;
        protected const int WaitTime = 100;
        protected const int LongWaitTime = 1000;
        protected async Task<bool> RunLoop(ISyncLock syncLock, int index, int [] releaseAtIndices, int maxCount = 15)
        {
            var counter = 0;
            bool? isLeader = null;
            var defaultLockTime = syncLock.LockExpiry;
            var lastLeaderAttempt = DateTime.MinValue;
            var releaseLock = false;
            while (counter < maxCount)
            {
                try
                {
                   // Acquire lock to ensure a single sequencer works at a specific time
                    bool acquired;
                    if (!isLeader.HasValue)
                    {
                        acquired = await syncLock.AcquireLock();
                    }
                    else if (isLeader.Value)
                    {
                        var remainingTime = syncLock.RemainingLockTime;
                        acquired = remainingTime > TimeSpan.Zero;
                        if (remainingTime < TimeSpan.FromMilliseconds(BufferTime))
                        {
                            var extended = await syncLock.ExtendLock(defaultLockTime);
                            //Console.WriteLine($"{GetTime()} ExtendLock={extended} #{index}");
                            if (!extended)
                            {
                                releaseLock = true;
                            }
                        }
                        else if (releaseAtIndices.Contains(counter))
                        {
                            releaseLock = true;
                        }
                    }
                    else
                    {
                        if (DateTime.UtcNow.Subtract(lastLeaderAttempt) > TimeSpan.FromMilliseconds(BufferTime))
                        {
                            acquired = await syncLock.AcquireLock();
                            lastLeaderAttempt = DateTime.UtcNow;
                        }
                        else
                        {
                            acquired = false;
                        }
                    }

                    if (!isLeader.HasValue || isLeader.Value != acquired)
                    {
                        //Console.WriteLine($"{GetTime()} AcquireLock={acquired} #{index}");
                    }

                    if (acquired == true)
                    {
                        isLeader = true;
                        await Task.Delay(WaitTime);

                        if (releaseLock)
                        {
                            var released = await syncLock.ReleaseLock();
                            if (released.HasValue && released.Value)
                            {
                                releaseLock = false;
                                isLeader = false;
                            }
                        }
                    }
                    else
                    {
                        isLeader = false;
                        await Task.Delay(WaitTime);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in RunLoop #{index}: {ex}");
                }
                finally
                {
                    counter++;
                }
            }
            return isLeader ?? false;
        }
    }
}
