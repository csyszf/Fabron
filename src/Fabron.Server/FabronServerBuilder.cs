using System;
using System.Collections.Generic;
using System.Reflection;
using Fabron.Core.CloudEvents;
using Fabron.Mando;
using Fabron.Store;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Hosting;

namespace Fabron;

public class FabronServerBuilder
{
    public FabronServerBuilder(IHostBuilder hostBuilder, IEnumerable<Assembly>? commandAssemblies = null)
    {
        if (hostBuilder.Properties.ContainsKey("HasFabronServerBuilder"))
        {
            throw new InvalidOperationException("FabronServerBuilder has already been added to this host.");
        }

        hostBuilder.Properties["HasFabronServerBuilder"] = "true";

        hostBuilder.ConfigureServices((ctx, services) =>
        {
            services.AddSingleton<IJobManager, JobManager>();
            services.AddScoped<IMediator, Mediator>()
                .RegisterJobCommandHandlers(commandAssemblies);
            services.AddSingleton<ISystemClock, SystemClock>();
            services.AddSingleton<IFabronClient, FabronClient>();
            services.AddSingleton<IEventDispatcher, EventDispatcher>();
        });

        HostBuilder = hostBuilder;
    }

    public IHostBuilder HostBuilder { get; }

    public FabronServerBuilder ConfigureOrleans(Action<HostBuilderContext, ISiloBuilder> configure)
    {
        HostBuilder.ConfigureServices((context, services) =>
        {
            services.AddOrleans(sb =>
            {
                configure(context, sb);
            });
        });

        return this;
    }

    public FabronServerBuilder UseLocalhostClustering()
    {
        ConfigureOrleans((ctx, sb) =>
        {
            sb.UseLocalhostClustering();
        });
        return this;
    }

    public FabronServerBuilder UseInMemory()
    {
        HostBuilder.ConfigureServices((context, services) =>
        {
            services.AddSingleton<IJobStore, InMemoryJobStore>();
            services.AddSingleton<ICronJobStore, InMemoryCronJobStore>();
            services.AddSingleton<ITimedEventStore, InMemoryTimedEventStore>();
            services.AddSingleton<ICronEventStore, InMemoryCronEventStore>();
        });

        ConfigureOrleans((context, siloBuilder) =>
        {
            siloBuilder
                .UseInMemoryReminderService();
        });

        return this;
    }

    public FabronServerBuilder AddSimpleEventRouter(Action<SimpleEventRouterOptions> configure)
    {
        HostBuilder.ConfigureServices((context, services) =>
        {
            services.AddSingleton<IEventRouter, SimpleEventRouter>();
            services.Configure<SimpleEventRouterOptions>(configure);
        });
        return this;
    }
}
