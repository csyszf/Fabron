// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Fabron;
using Fabron.ElasticSearch;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

using Nest;

namespace Microsoft.Extensions.DependencyInjection
{

    public static class FabronElasticSearchServiceCollectionExtensions
    {
        public static IServiceCollection UseElasticSearch(this IServiceCollection services, IConfiguration config)
        {
            services.Configure<ElasticSearchOptions>(config);
            services.AddElasticSearchJobReporter();
            services.AddElasticSearchJobQuerier();
            return services;
        }

        public static IServiceCollection UseElasticSearch(this IServiceCollection services, Action<ElasticSearchOptions> configure)
        {
            services.Configure<ElasticSearchOptions>(configure);
            services.AddElasticSearchJobReporter();
            return services;
        }

        internal static IServiceCollection AddElasticSearchJobQuerier(this IServiceCollection services)
        {
            services.AddSingleton<IJobQuerier, ElasticSearchJobQuerier>(sp =>
            {
                ElasticSearchOptions options = sp.GetRequiredService<IOptions<ElasticSearchOptions>>().Value;
                ConnectionSettings settings = new ConnectionSettings(new Uri(options.Server));
                ElasticClient client = new(settings);
                return ActivatorUtilities.CreateInstance<ElasticSearchJobQuerier>(sp, client);
            });
            return services;
        }

        internal static IServiceCollection AddElasticSearchJobReporter(this IServiceCollection services)
        {
            services.AddSingleton<IJobReporter, ElasticSearchJobReporter>(sp =>
            {
                ElasticSearchOptions options = sp.GetRequiredService<IOptions<ElasticSearchOptions>>().Value;
                ConnectionSettings settings = new ConnectionSettings(new Uri(options.Server));
                ElasticClient client = new(settings);
                return ActivatorUtilities.CreateInstance<ElasticSearchJobReporter>(sp, client);
            });
            return services;
        }
    }
}
