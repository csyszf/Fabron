using System;
using Fabron.Stores;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Hosting;

namespace Fabron.Providers.PostgreSQL
{
    public static class FabronPosgreSQLExtensions
    {
        const string Invariant = "Npgsql";
        public static IClientBuilder UsePostgreSQLClustering(this IClientBuilder client, string connectionString)
        {
            client.UseAdoNetClustering(options =>
            {
                options.Invariant = Invariant;
                options.ConnectionString = connectionString;
            });
            return client;
        }

        public static ISiloBuilder UsePosgreSQL(this ISiloBuilder silo, IConfiguration configuration)
        {
            silo.Configure<PostgreSQLOptions>(configuration);
            silo.UsePosgreSQL(configuration["ConnectionString"]);
            return silo;
        }

        public static ISiloBuilder UsePosgreSQL(this ISiloBuilder silo, Action<PostgreSQLOptions> configureOptions)
        {
            silo.Configure(configureOptions);
            var options = new PostgreSQLOptions();
            configureOptions(options);
            silo.UsePosgreSQL(options.ConnectionString);
            return silo;
        }
        public static ISiloBuilder UsePosgreSQL(this ISiloBuilder silo, string connectionString)
        {
            silo.UseAdoNetClustering(options =>
            {
                options.Invariant = Invariant;
                options.ConnectionString = connectionString;
            });
            silo.UseAdoNetReminderService(options =>
            {
                options.Invariant = Invariant;
                options.ConnectionString = connectionString;
            });
            silo.UsePostgreSQLEventStores();
            return silo;
        }

        public static ISiloBuilder UsePostgreSQLEventStores(this ISiloBuilder silo, Action<PostgreSQLOptions> configureOptions)
        {
            silo.Configure(configureOptions);
            silo.UsePostgreSQLEventStores();
            return silo;
        }
        public static ISiloBuilder UsePostgreSQLEventStores(this ISiloBuilder silo)
        {
            silo.ConfigureServices(services =>
            {
                services.AddSingleton<IJobEventStore, PostgreSQLJobEventStore>();
                services.AddSingleton<ICronJobEventStore, PostgreSQLCronJobEventStore>();
            });
            return silo;
        }
    }

}
