using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
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

    public class PostgreSQLCronJobEventStore : PostgreSQLEventStore, ICronJobEventStore
    {
        public PostgreSQLCronJobEventStore(IOptions<PostgreSQLOptions> options)
            : base(options.Value.ConnectionString, options.Value.CronJobEventLogsTableName, options.Value.CronJobConsumersTableName)
        { }
    }

    public class PostgreSQLEventStore : IEventStore
    {
        private readonly string _connStr;
        private readonly string _eventLogsTableName;
        private readonly string _consumersTableName;
        private readonly string _sql_GetEventLogs;
        private readonly string _sql_CommitEventLog;
        private readonly string _sql_ClearEventLogs;
        private readonly string _sql_GetConsumerOffset;
        private readonly string _sql_SaveConsumerOffset;
        private readonly string _sql_ClearConsumerOffset;

        public PostgreSQLEventStore(string connStr, string eventLogsTableName, string consumersTableName)
        {
            _connStr = connStr;
            _eventLogsTableName = eventLogsTableName;
            _consumersTableName = consumersTableName;

            _sql_GetEventLogs = $@"
SELECT *
FROM {_eventLogsTableName}
WHERE entity_key = @entityKey
    AND version >= @minVersion
";

            _sql_CommitEventLog = $@"
INSERT INTO {_eventLogsTableName} (
    entity_key,
    version,
    timestamp,
    type,
    data
)
VALUES (
    @EntityKey,
    @Version,
    @Timestamp,
    @Type,
    @Data
)
";

            _sql_ClearEventLogs = $@"
DELETE FROM {_eventLogsTableName}
WHERE
    entity_key = @entityKey
";

            _sql_GetConsumerOffset = $@"
SELECT _offset
FROM {consumersTableName}
WHERE entity_key = @entityKey
LIMIT 1
";

            _sql_SaveConsumerOffset = $@"
INSERT INTO {consumersTableName} (entity_key, _offset)
VALUES(@entityKey, @offset)
ON CONFLICT (entity_key)
DO
    UPDATE SET _offset = EXCLUDED._offset;
";

            _sql_ClearConsumerOffset = $@"
DELETE FROM {consumersTableName}
WHERE
    entity_key = @entityKey
";
        }

        public async Task<List<EventLog>> GetEventLogs(string entityKey, long minVersion)
        {
            var result = new List<EventLog>();

            await using var conn = new NpgsqlConnection(_connStr);
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand(_sql_GetEventLogs, conn);
            cmd.Parameters.AddWithValue("@entityKey", entityKey);
            cmd.Parameters.AddWithValue("@minVersion", minVersion);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                result.Add(new EventLog(
                    reader.GetString(0),
                    reader.GetInt64(1),
                    reader.GetDateTime(2),
                    reader.GetString(3),
                    reader.GetString(4)));

            return result;
        }

        public async Task CommitEventLog(EventLog eventLog)
        {
            await using var conn = new NpgsqlConnection(_connStr);
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand(_sql_CommitEventLog, conn);
            cmd.Parameters.AddWithValue("@EntityKey", eventLog.EntityKey);
            cmd.Parameters.AddWithValue("@Version", eventLog.Version);
            cmd.Parameters.AddWithValue("@Timestamp", eventLog.Timestamp);
            cmd.Parameters.AddWithValue("@Type", eventLog.Type);
            cmd.Parameters.AddWithValue("@Data", eventLog.Data);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task ClearEventLogs(string entityKey, long maxVersion)
        {
            await using var conn = new NpgsqlConnection(_connStr);
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand(_sql_ClearEventLogs, conn);
            cmd.Parameters.AddWithValue("@entityKey", entityKey);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<long> GetConsumerOffset(string entityKey)
        {
            await using var conn = new NpgsqlConnection(_connStr);
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand(_sql_GetConsumerOffset, conn);
            cmd.Parameters.AddWithValue("@entityKey", entityKey);
            await using var reader = await cmd.ExecuteReaderAsync();
            await reader.ReadAsync();
            if(reader.HasRows)
                return reader.GetInt64(0);
            else return -1L;
        }

        public async Task SaveConsumerOffset(string entityKey, long offset)
        {
            await using var conn = new NpgsqlConnection(_connStr);
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand(_sql_SaveConsumerOffset, conn);
            cmd.Parameters.AddWithValue("@entityKey", entityKey);
            cmd.Parameters.AddWithValue("@offset", offset);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task ClearConsumerOffset(string entityKey)
        {
            await using var conn = new NpgsqlConnection(_connStr);
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand(_sql_ClearConsumerOffset, conn);
            cmd.Parameters.AddWithValue("@entityKey", entityKey);
            await cmd.ExecuteNonQueryAsync();
        }

    }
}
