using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Orleans;
using Orleans.Hosting;

namespace Fabron;

public class FabronClientBuilder
{
    public FabronClientBuilder(
        IHostBuilder hostBuilder,
        IEnumerable<Assembly>? commandAssemblies,
        bool cohosted)
    {
        if (hostBuilder.Properties.ContainsKey("HasFabronClientBuilder"))
        {
            throw new InvalidOperationException("FabronClientBuilder has already been added to this host.");
        }

        hostBuilder.Properties["HasFabronClientBuilder"] = "true";

        hostBuilder.ConfigureServices((ctx, services) =>
        {
            services.TryAddSingleton<IJobManager, JobManager>();
            services.TryAddSingleton<IFabronClient, FabronClient>();
            services.RegisterCommands(commandAssemblies);
        });

        HostBuilder = hostBuilder;
        Cohosted = cohosted;

        if (!cohosted)
        {
            hostBuilder.UseOrleansClient((ctx, cb) => { });
        }
    }

    public IHostBuilder HostBuilder { get; }

    public bool Cohosted { get; }

    public FabronClientBuilder ConfigureOrleansClient(Action<HostBuilderContext, IClientBuilder> configure)
    {
        if (Cohosted) throw new InvalidOperationException("Cannot configure Orleans client when cohosted.");
        HostBuilder.ConfigureServices((context, services) =>
        {
            services.AddOrleansClient(sb =>
            {
                configure(context, sb);
            });
        });

        return this;
    }

    public FabronClientBuilder UseLocalhostClustering()
    {
        ConfigureOrleansClient((ctx, cb) =>
        {
            cb.UseLocalhostClustering();
        });
        return this;
    }
}
