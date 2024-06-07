using Xunit;
using Moq;

namespace RedisAccessLayer.Tests
{
    public abstract class MasterSlaveFakeBaseTests : BasicMockTests
    {
        protected static string GetTime()
        {
            return DateTime.UtcNow.ToString("HH:mm:ss.fff");
        }

        protected const int BufferTime = 100;
        protected const int WaitTime = 100;
        protected const int LongWaitTime = 1000;
        protected async Task<bool> RunLoop(ISyncLock syncLock, int index, int [] releaseAtIndices)
        {
            const int MaxCount = 15;
            var counter = 0;
            bool? isMaster = null;
            var defaultLockTime = syncLock.LockExpiry;
            var lastMasterAttempt = DateTime.MinValue;
            var releaseLock = false;
            while (counter < MaxCount)
            {
                try
                {
                   // Acquire lock to ensure a single sequencer works at a specific time
                    bool acquired;
                    if (!isMaster.HasValue)
                    {
                        acquired = await syncLock.AcquireLock();
                    }
                    else if (isMaster.Value)
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
                        if (DateTime.UtcNow.Subtract(lastMasterAttempt) > TimeSpan.FromMilliseconds(BufferTime))
                        {
                            acquired = await syncLock.AcquireLock();
                            lastMasterAttempt = DateTime.UtcNow;
                        }
                        else
                        {
                            acquired = false;
                        }
                    }

                    if (!isMaster.HasValue || isMaster.Value != acquired)
                    {
                        //Console.WriteLine($"{GetTime()} AcquireLock={acquired} #{index}");
                    }

                    if (acquired == true)
                    {
                        isMaster = true;
                        await Task.Delay(WaitTime);

                        if (releaseLock)
                        {
                            var released = await syncLock.ReleaseLock();
                            if (released.HasValue && released.Value)
                            {
                                releaseLock = false;
                                isMaster = false;
                            }
                        }
                    }
                    else
                    {
                        isMaster = false;
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
            return isMaster ?? false;
        }
    }
}
