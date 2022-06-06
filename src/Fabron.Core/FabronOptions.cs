using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cronos;

namespace Fabron
{
    public class CommonOptions
    {
        public JsonSerializerOptions JsonSerializerOptions { get; set; }
            = new(JsonSerializerDefaults.Web)
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            };

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

    public class FabronClientOptions : CommonOptions
    {
    }

    public class SimpleSchedulerOptions : CommonOptions
    {
    }

    public class CronSchedulerOptions : CommonOptions
    {
        public CronFormat CronFormat { get; set; } = CronFormat.Standard;
    }

}
