
using System;
using Fabron.Mando;
using Fabron.Store;
using Microsoft.Extensions.DependencyInjection;

using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.TestingHost;

namespace Fabron.FunctionalTests
{
    public class TestSiloConfigurator : ISiloConfigurator
    {
        public virtual void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpClient();
        }

        public virtual void ConfigureSilo(ISiloBuilder siloBuilder)
        {
        }

        public void Configure(ISiloBuilder siloBuilder)
        {
            siloBuilder.Configure<MessagingOptions>(options =>
            {
                options.ResponseTimeout = TimeSpan.FromSeconds(5);
            });

            siloBuilder.UseLocalhostClustering()
                .UseInMemoryReminderService();
            siloBuilder.ConfigureServices(services =>
            {
                services.AddSingleton<IJobStore, InMemoryJobStore>();
                services.AddSingleton<ICronJobStore, InMemoryCronJobStore>();
                services.AddScoped<IMediator, Mediator>()
                    .RegisterJobCommandHandlers();
                services.AddSingleton<ISystemClock, SystemClock>();
            });

            siloBuilder.ConfigureServices(ConfigureServices);
        }
    }
}
