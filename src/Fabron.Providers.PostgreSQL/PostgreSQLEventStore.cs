using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Fabron.Events;
using Fabron.Stores;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Fabron.Providers.PostgreSQL
{
    public class PostgreSQLJobEventStore : PostgreSQLEventStore, IJobEventStore
    {
        public PostgreSQLJobEventStore(IOptions<PostgreSQLOptions> options)
            : base(options.Value.ConnectionString, options.Value.CronJobEventLogsTableName, options.Value.CronJobConsumersTableName)
        { }

    }
    public class PostgreSQLEventStore : IEventStore
    {
        private readonly string _connStr;
        private readonly string _eventLogsTableName;
        private readonly string _consumersTableName;

        public PostgreSQLEventStore(string connStr, string eventLogsTableName, string consumersTableName)
        {
            _connStr = connStr;
            _eventLogsTableName = eventLogsTableName;
            _consumersTableName = consumersTableName;
        }

        public async Task<List<EventLog>> GetEventLogs(string entityKey, long minVersion)
        {
            using var conn = new NpgsqlConnection(_connStr);
            const string sql = @"
SELECT *
FROM @tableName
WHERE EntityKey = @entityKey
    AND Version >= @minVersion
";
            var eventLogs = await conn.QueryAsync<EventLog>(sql, new
            {
                tableName = _eventLogsTableName,
                entityKey,
                minVersion
            });
            return eventLogs.ToList();
        }

        public async Task CommitEventLog(EventLog eventLog)
        {
            using var conn = new NpgsqlConnection(_connStr);
            const string sql = @"
INSERT INTO @tableName (
    EntityKey,
    Version,
    Timestamp,
    Type,
    Data,
)
VALUES (
    @EntityKey,
    @Version,
    @Timestamp,
    @Type,
    @Data
)
";
            await conn.ExecuteAsync(sql, new
            {
                tableName = _eventLogsTableName,
                eventLog.EntityKey,
                eventLog.Version,
                eventLog.Timestamp,
                eventLog.Type,
                eventLog.Data
            });
        }

        public async Task ClearEventLogs(string entityKey, long maxVersion)
        {
            using var conn = new NpgsqlConnection(_connStr);
            const string sql = @"
DELETE FROM @tableName
WHERE
    EntityKey = @entityKey
";
            await conn.ExecuteAsync(sql, new { tableName = _eventLogsTableName, entityKey });
        }


        public async Task<long> GetConsumerOffset(string entityKey)
        {
            using var conn = new NpgsqlConnection(_connStr);
            const string sql = @"
SELECT *
FROM @tableName
WHERE EntityKey = @entityKey
";
            var consumer = await conn.QueryFirstOrDefaultAsync<ConsumerRow>(sql, new
            {
                tableName = _consumersTableName,
                entityKey,
            });
            return consumer is null ? -1L : consumer.Offset;
        }

        public async Task SaveConsumerOffset(string entityKey, long offset)
        {
            using var conn = new NpgsqlConnection(_connStr);
            const string sql = @"
INSERT INTO @tableName (EntityKey, Offset)
VALUES(@entityKey, @offset)
ON CONFLICT (EntityKey)
DO
    UPDATE SET Offset = EXCLUDED.Offset;
";
            await conn.ExecuteAsync(sql, new
            {
                tableName = _consumersTableName,
                entityKey,
                offset
            });
        }

        public async Task ClearConsumerOffset(string entityKey)
        {
            using var conn = new NpgsqlConnection(_connStr);
            const string sql = @"
DELETE FROM @tableName
WHERE
    EntityKey = @entityKey
";
            await conn.ExecuteAsync(sql, new
            {
                tableName = _consumersTableName,
                entityKey,
            });
        }

    }
}
