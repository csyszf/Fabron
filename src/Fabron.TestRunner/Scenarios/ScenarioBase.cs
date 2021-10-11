using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fabron.Grains;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;

namespace Fabron.TestRunner.Scenarios
{

    public abstract class ScenarioBase
    {
        private IHost _host = default!;
        private IServiceProvider ServiceProvider => _host.Services;
        public IJobManager JobManager => ServiceProvider.GetRequiredService<IJobManager>();
        public IClusterClient ClusterClient => ServiceProvider.GetRequiredService<IClusterClient>();
        public IGrainFactory GrainFactory => ClusterClient;

        public ICronJobGrain GetCronJobGrain(string id) => ClusterClient.GetGrain<ICronJobGrain>(id);

        public virtual IHostBuilder ConfigureHost(IHostBuilder builder)
        {
            return builder;
        }

        public virtual ISiloBuilder ConfigureSilo(ISiloBuilder builder)
        {
            return builder;
        }

        protected virtual IEnumerable<KeyValuePair<string, string>> Configs => new Dictionary<string, string>
        {
            { "Logging:LogLevel:Default", "Error" },
        };

        public virtual IHost CreateHost()
        {
            var builder = Host.CreateDefaultBuilder()
                .ConfigureHostConfiguration(config =>
                {
                    config.AddInMemoryCollection(Configs);
                    // config.AddInMemoryCollection(new Dictionary<string, string>
                    //     {
                    //         { "Logging:LogLevel:Default", "Error" },
                    //         // { "Logging:LogLevel:Fabron.Grains.CronJobGrain", "Debug" },
                    //         // { "Logging:LogLevel:Fabron", "Debug" },
                    //         { "Logging:LogLevel:Fabron.Grains.CronJobScheduler", "Debug" },
                    //         // { "Logging:LogLevel:Fabron.ElasticSearch.ElasticSearchJobQuerier", "Debug" }
                    //     });
                })
                .ConfigureServices(services =>
                {
                    services.AddFabron();
                });

            builder = ConfigureHost(builder);

            builder.UseFabron((context, silo) =>
            {
                silo
                    .Configure<StatisticsOptions>(options =>
                    {
                        options.LogWriteInterval = TimeSpan.FromMilliseconds(-1);
                    })
                    .UseLocalhostClustering()
                    .UseInMemory();
                ConfigureSilo(silo);
            }
            )
            .UseConsoleLifetime();
            return builder.Build();
        }

        public abstract Task RunAsync();

        public async Task PlayAsync()
        {
            _host = CreateHost();
            await _host.StartAsync();
            await RunAsync();
            await _host.StopAsync().WaitAsync(TimeSpan.FromSeconds(3));
            Console.WriteLine("FINISHED");
        }
    }
}
