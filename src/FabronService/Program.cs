
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using AspNetCore.Authentication.ApiKey;
using Fabron.Providers.PostgreSQL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Npgsql;
using OpenTelemetry.Trace;
using Orleans.Hosting;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

var server = builder.Host.UseFabronServer();
var client = builder.Host.UseFabronClient(cohosted: true);

if (builder.Environment.IsDevelopment())
{
    server.UseLocalhostClustering()
        .UseInMemory();
}
else
{
    server
        .ConfigureOrleans((ctx, siloBuilder) =>
        {
            siloBuilder.UseKubernetesHosting();
            siloBuilder.AddActivityPropagation();
        })
        .UsePostgreSQL(builder.Configuration["PGSQL"]);
    client.UsePostgreSQL(builder.Configuration["PGSQL"]);
}

builder.Services
    .ConfigureFramework()
    .AddApiKeyAuth(builder.Configuration["ApiKey"])
    .AddSwagger()
    .RegisterJobCommandHandlers();

builder.Services
    .AddOpenTelemetryTracing(options => options
        .AddEncrichedAspNetCoreInstrumentation()
        .AddNpgsql()
        .AddSource("Microsoft.Orleans")
        .AddProcessor()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter()
    );

var app = builder.Build();

app.UseCustomSwagger();

app.UseAuthentication()
    .UseAuthorization();

app.MapHealthChecks("/health").AllowAnonymous();
app.MapMetrics().AllowAnonymous();
app.MapCronHttpReminders();
app.MapHttpReminders();

app.Run();

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

    public static IServiceCollection AddApiKeyAuth(this IServiceCollection services, string validApiKey)
    {
        services.AddAuthentication(ApiKeyDefaults.AuthenticationScheme)
            .AddApiKeyInAuthorizationHeader(options =>
            {
                options.Realm = "FabronService API";
                options.KeyName = "token";
                options.IgnoreAuthenticationIfAllowAnonymous = true;
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
