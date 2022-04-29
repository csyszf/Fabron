
using System;
using Microsoft.Extensions.DependencyInjection;

using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.TestingHost;

namespace Fabron.FunctionalTests
{
    public class TestSiloConfigurator : ISiloConfigurator
    {
        public virtual void ConfigureServices(IServiceCollection services) => services.AddHttpClient();

        public virtual void ConfigureSilo(ISiloBuilder siloBuilder)
        {
        }

        public void Configure(ISiloBuilder siloBuilder)
        {
            siloBuilder.Configure<MessagingOptions>(options =>
            {
                options.ResponseTimeout = TimeSpan.FromSeconds(5);
            });

            siloBuilder.Configure<CronJobOptions>(options =>
            {
                options.UseAsynchronousIndexer = true;
            });
            siloBuilder.Configure<JobOptions>(options =>
            {
                options.UseAsynchronousIndexer = true;
            });
            siloBuilder.UseInMemory();

            siloBuilder.ConfigureServices(ConfigureServices);
            siloBuilder.AddFabron();
        }
    }
}
