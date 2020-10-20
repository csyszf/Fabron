using System;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using TGH.Server.Grains;

namespace TGH.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseOrleans(siloBuilder =>
                {
                    siloBuilder
                        .UseLocalhostClustering()
                        .Configure<HostOptions>(options => options.ShutdownTimeout = TimeSpan.FromMinutes(1))
                        .Configure<ClusterOptions>(opts =>
                        {
                            opts.ClusterId = "dev";
                            opts.ServiceId = "TGH";
                        })
                        .Configure<EndpointOptions>(opts =>
                        {
                            opts.AdvertisedIPAddress = IPAddress.Loopback;
                        })
                        .Configure<StatisticsOptions>(opts =>
                        {
                            //opts.LogWriteInterval = TimeSpan.FromMinutes(5);
                            //opts.CollectionLevel = Orleans.Runtime.Configuration.StatisticsLevel.Verbose;
                        })
                        .UseInMemoryReminderService()
                        .AddMemoryGrainStorage("JobStore")
                        // .AddAdoNetGrainStorage("JobStore", (AdoNetGrainStorageOptions opt)=>
                        // {
                        //     opt.Invariant = "Microsoft.Data.SqlClient";
                        //     opt.ConnectionString = @"Data Source=(localdb)\MSSQLLocalDB;Database=TGH;Integrated Security=True;
                        //         Max Pool Size=200; MultipleActiveResultSets=True";
                        // })
                        // .UseAdoNetReminderService((AdoNetReminderTableOptions opt)=>
                        // {
                        //     opt.Invariant = "Microsoft.Data.SqlClient";
                        //     opt.ConnectionString = @"Data Source=(localdb)\MSSQLLocalDB;Database=TGH;Integrated Security=True;
                        //         Max Pool Size=200; MultipleActiveResultSets=True";
                        // })
                        .ConfigureApplicationParts(manager =>
                        {
                            //manager.AddApplicationPart(typeof(JobGrain<,>).Assembly).WithReferences();
                        });
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
