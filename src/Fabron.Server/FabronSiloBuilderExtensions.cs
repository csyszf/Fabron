// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;

using Fabron;

using Microsoft.Extensions.DependencyInjection;

namespace Orleans.Hosting
{
    public static class FabronSiloBuilderExtensions
    {
        public static ISiloBuilder AddFabron(this ISiloBuilder siloBuilder, IEnumerable<Assembly>? commandAssemblies = null)
        {
            siloBuilder
                .ConfigureServices((ctx, services) =>
                {
                    services.AddFabronCore();
                });

            siloBuilder.ConfigureServices((ctx, services) =>
            {
                services.RegisterJobCommandHandlers(commandAssemblies);
                services.AddJobReporter<NoopJobReporter>();
                services.AddJobQuerier<NoopJobQuerier>();
            });

            return siloBuilder;
        }

        public static ISiloBuilder UseInMemoryJobStore(this ISiloBuilder siloBuilder)
        {
            siloBuilder
                .UseInMemoryReminderService()
                .AddMemoryGrainStorage("JobStore");
            return siloBuilder;
        }
    }
}
