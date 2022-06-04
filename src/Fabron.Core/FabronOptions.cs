using System;
using Cronos;

namespace Fabron
{
    public class CommonOptions
    {
        public bool UseSynchronousTicker { get; set; } = false;
        public TimeZoneInfo TimeZone { get; set; } = TimeZoneInfo.Utc;
        public TimeSpan TickerInterval { get; set; } = TimeSpan.FromMinutes(2);
    }

    public class JobOptions : CommonOptions
    {
    }

    public class CronJobOptions : CommonOptions
    {
        public CronFormat CronFormat { get; set; } = CronFormat.Standard;
    }

    public class SimpleSchedulerOptions : CommonOptions
    {
    }

    public class CronSchedulerOptions : CommonOptions
    {
        public CronFormat CronFormat { get; set; } = CronFormat.Standard;
    }

}
