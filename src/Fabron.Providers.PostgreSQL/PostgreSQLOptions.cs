namespace Fabron.Providers.PostgreSQL
{
    public class PostgreSQLOptions
    {
        public string ConnectionString { get; set; } = default!;

        public string JobEventLogsTableName { get; set; } = "JobEventLogs";

        public string CronJobEventLogsTableName { get; set; } = "CronJobEventLogs";

        public string JobConsumersTableName { get; set; } = "JobConsumers";

        public string CronJobConsumersTableName { get; set; } = "CronJobConsumers";

    }

}
