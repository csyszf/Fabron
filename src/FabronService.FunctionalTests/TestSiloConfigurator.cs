// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Fabron;

using Microsoft.Extensions.DependencyInjection;

using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.TestingHost;

namespace FabronService.FunctionalTests
{
    public class TestSiloConfigurator : ISiloConfigurator
    {
        public virtual void ConfigureServices(Microsoft.Extensions.Hosting.HostBuilderContext context, IServiceCollection services) => services.AddHttpClient();

        public void Configure(ISiloBuilder siloBuilder)
        {
            siloBuilder.Configure<MessagingOptions>(options =>
            {
                options.ResponseTimeout = TimeSpan.FromSeconds(5);
            });
            siloBuilder.UseInMemoryJobStore()
                .AddMemoryGrainStorageAsDefault();
            siloBuilder.ConfigureServices(services =>
            {
                services.AddJobReporter<NoopJobReporter>();
                services.AddJobQuerier<NoopJobQuerier>();
            });
            siloBuilder.ConfigureServices(ConfigureServices);
            siloBuilder.AddFabron();
        }
    }
}
