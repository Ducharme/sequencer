using log4net;

namespace CommonServiceLib
{
    public class CpuInfo
    {
        private const string CGroupQuotaFile = "/sys/fs/cgroup/cpu/cpu.cfs_quota_us";
        private const string CGroupPeriodFile = "/sys/fs/cgroup/cpu/cpu.cfs_period_us";
        private const string CGroupCpuMaxFile = "/sys/fs/cgroup/cpu.max";
        private const string ProcCpuInfoFile = "/proc/cpuinfo";
        private static readonly ILog logger = LogManager.GetLogger(typeof(CpuInfo));

        public double GetCpuCount()
        {
            if (OperatingSystem.IsWindows())
            {
                logger.Debug("Getting CPU count from Environment.ProcessorCount");
                return Environment.ProcessorCount;
            }
            
            if (OperatingSystem.IsLinux())
            {
                try
                {
                    // Check container CPU limits first
                    if (File.Exists(CGroupQuotaFile) && File.Exists(CGroupPeriodFile))
                    {
                        var quota = long.Parse(File.ReadAllText(CGroupQuotaFile));
                        var period = long.Parse(File.ReadAllText(CGroupPeriodFile));
                        if (quota > 0 && period > 0)
                        {
                            logger.Debug("Getting CPU count from cgroup quota and period");
                            return (double)quota / period;
                        }
                    }

                    // Alternative cgroup v2 path
                    if (File.Exists(CGroupCpuMaxFile))
                    {
                        var cpuMax = File.ReadAllText(CGroupCpuMaxFile).Split(' ');
                        if (cpuMax.Length >= 2 && long.TryParse(cpuMax[0], out var quota) && 
                            long.TryParse(cpuMax[1], out var period))
                        {
                            if (quota > 0 && period > 0)
                            {
                                logger.Debug("Getting CPU count from cgroup cpu.max");
                                return (double)quota / period;
                            }
                        }
                    }

                    // Fallback to /proc/cpuinfo for physical CPU count
                    if (File.Exists(ProcCpuInfoFile))
                    {
                        logger.Debug("Getting CPU count from /proc/cpuinfo");
                        var cpuInfo = File.ReadAllText(ProcCpuInfoFile);
                        return cpuInfo.Split('\n').Count(line => line.StartsWith("processor"));
                    }
                }
                catch
                {
                    logger.Debug("Getting CPU count Environment.ProcessorCount as fallback");
                    // Fallback to Environment.ProcessorCount if all else fails
                }
            }

            return Environment.ProcessorCount;
        }
    }
}