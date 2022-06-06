
using System;
using System.Net.Http;

using FakeItEasy;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

using Moq;

using Orleans.TestingHost;

namespace FabronService.FunctionalTests
{
    public static class WAFExtensions
    {
        public static WebApplicationFactory<Program> WithTestUser(this WebApplicationFactory<Program> waf)
            => waf.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddAuthentication("Test")
                        .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                            "Test", options => { });
                });
            });

        public static TestCluster GetSiloCluster(this WebApplicationFactory<Program> waf)
            => waf.Services.GetRequiredService<TestCluster>();

        public static TService GetSiloService<TService>(this WebApplicationFactory<Program> waf)
            where TService : notnull
            => ((InProcessSiloHandle)waf.GetSiloCluster().Primary).SiloHost.Services.GetRequiredService<TService>();

        public static Mock<HttpMessageHandler> GetHttpMessageHandlerMock(this WebApplicationFactory<Program> waf)
            => waf.GetSiloService<Mock<HttpMessageHandler>>();

        public static WebApplicationFactory<Program> WithServices(this WebApplicationFactory<Program> waf, Action<IServiceCollection> configureServices) => waf.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(configureServices);
        });

        public static WebApplicationFactory<Program> WithFakes(this WebApplicationFactory<Program> waf, params object[] fakes) => waf.WithServices(services =>
        {
            foreach (object? fake in fakes)
            {
                Type? fakeType = Fake.GetFakeManager(fake).FakeObjectType;
                services.AddScoped(fakeType, sp => fake);
            }
        });

        public static (HttpClient httpClient, TService fake) CreateClient<TService>(this WebApplicationFactory<Program> waf, WebApplicationFactoryClientOptions clientOptions)
            where TService : class
        {
            TService? fake = A.Fake<TService>();
            WebApplicationFactory<Program>? newWaf = waf.WithFakes(fake);
            HttpClient? httpClient = newWaf.CreateClient(clientOptions);
            return (httpClient, fake);
        }

        public static (HttpClient httpClient, TService1 fake1, TService2 fake2) CreateClient<TService1, TService2>(this WebApplicationFactory<Program> waf, WebApplicationFactoryClientOptions clientOptions)
            where TService1 : class
            where TService2 : class
        {
            TService1? fake1 = A.Fake<TService1>();
            TService2? fake2 = A.Fake<TService2>();
            WebApplicationFactory<Program>? newWaf = waf
                .WithFakes(fake1, fake2);
            HttpClient? httpClient = newWaf.CreateClient(clientOptions);
            return (httpClient, fake1, fake2);
        }

        public static HttpClient CreateClient(this WebApplicationFactory<Program> waf, WebApplicationFactoryClientOptions clientOptions, params object[] fakes)
        {
            WebApplicationFactory<Program>? newWaf = waf
                .WithFakes(fakes);
            HttpClient? httpClient = newWaf.CreateClient(clientOptions);
            return httpClient;
        }

    }
}
