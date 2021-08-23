// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using AspNetCore.Authentication.ApiKey;

using FabronService.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

using Newtonsoft.Json;

using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Providers.MongoDB.Configuration;

using Prometheus;

IHostBuilder hostBuilder = Host.CreateDefaultBuilder(args)
    .UseFabron((ctx, siloBuilder) =>
    {
        siloBuilder.AddPrometheusTelemetryConsumer()
            .Configure<StatisticsOptions>(options =>
            {
                options.LogWriteInterval = TimeSpan.FromMilliseconds(-1);
            });
        if (ctx.HostingEnvironment.IsEnvironment("Localhost"))
        {
            siloBuilder.UseLocalhostClustering()
                .AddMemoryGrainStorageAsDefault()
                .UseInMemoryJobStore();
        }
        else
        {
            siloBuilder.UseKubernetesHosting()
                .UseMongoDBClient(ctx.Configuration["MongoDbConnectionString"])
                .UseMongoDBClustering(options =>
                {
                    options.DatabaseName = "Fabron";
                    options.Strategy = MongoDBMembershipStrategy.SingleDocument;
                })
                .UseMongoDBReminders(options =>
                {
                    options.DatabaseName = "Fabron";
                })
                .AddMongoDBGrainStorage("JobStore", configure =>
                {
                    configure.Configure(options =>
                    {
                        options.DatabaseName = "Fabron";
                        options.ConfigureJsonSerializerSettings = settings =>
                        {
                            settings.NullValueHandling = NullValueHandling.Include;
                            settings.ObjectCreationHandling = ObjectCreationHandling.Replace;
                            settings.DefaultValueHandling = DefaultValueHandling.Populate;
                        };
                    });
                })
                .AddMongoDBGrainStorageAsDefault(configure =>
                {
                    configure.Configure(options =>
                    {
                        options.DatabaseName = "Fabron";
                        options.ConfigureJsonSerializerSettings = settings =>
                        {
                            settings.NullValueHandling = NullValueHandling.Include;
                            settings.ObjectCreationHandling = ObjectCreationHandling.Replace;
                            settings.DefaultValueHandling = DefaultValueHandling.Populate;
                        };
                    });
                })
                .ConfigureServices(services =>
                {
                    services.UseElasticSearch(ctx.Configuration.GetSection("Reporters:ElasticSearch"));
                }); ;
        }
    })
    .ConfigureWebHostDefaults(builder =>
    {
        builder
            .ConfigureServices(ConfigureServices)
            .Configure(ConfigureWebApplication);
    });
IHost host = hostBuilder.Build();
await host.RunAsync();


static void ConfigureServices(WebHostBuilderContext context, IServiceCollection services)
{
    services.ConfigureFramework()
        .AddApiKeyAuth(context.Configuration["ApiKey"])
        .AddSwagger();
    services.RegisterJobCommandHandlers();
}

static void ConfigureWebApplication(IApplicationBuilder app)
{
    app.UseCustomSwagger()
        .UseRouting()
        .UseAuthentication()
        .UseAuthorization()
        .UseEndpoints(endpoints =>
        {
            endpoints.MapHealthChecks("/health")
                .AllowAnonymous();
            endpoints.MapMetrics();
            endpoints.MapControllers()
                .RequireAuthorization();
        });
}


#pragma warning disable CA1050 // Declare types in namespaces
public static class AppConfigureExtensions
#pragma warning restore CA1050 // Declare types in namespaces
{
    public static IServiceCollection ConfigureFramework(this IServiceCollection services)
    {
        services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        });
        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });
        services.AddHttpClient();
        services.AddHealthChecks();
        services.AddSingleton<IResourceLocator, ResourceLocator>();
        return services;
    }

    public static IServiceCollection AddSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "FabronService", Version = "v1" });
        });
        return services;
    }

    public static IServiceCollection AddApiKeyAuth(this IServiceCollection services, Configuration configuration)
    {
        services.AddApiKeyAuth(configuration["ApiKey"]);
        return services;
    }
    public static IServiceCollection AddApiKeyAuth(this IServiceCollection services, string validApiKey)
    {
        services.AddAuthentication(ApiKeyDefaults.AuthenticationScheme)
            .AddApiKeyInAuthorizationHeader(options =>
            {
                options.Realm = "FabronService API";
                options.KeyName = "token";
                options.Events = new ApiKeyEvents
                {
                    OnValidateKey = ctx =>
                    {
                        if (ctx.ApiKey == validApiKey)
                        {
                            ctx.ValidationSucceeded("debug");
                        }
                        else
                        {
                            ctx.ValidationFailed();
                        }
                        return Task.CompletedTask;
                    }
                };
            });
        return services;
    }

    public static IApplicationBuilder UseCustomSwagger(this IApplicationBuilder app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "FabronService v1"));
        return app;
    }
}
