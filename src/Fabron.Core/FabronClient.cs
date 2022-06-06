using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Fabron.Core.CloudEvents;
using Fabron.Grains;
using Fabron.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;

namespace Fabron;

public interface IFabronClient
{
    Task ScheduleTimedEvent<T>(
        string key,
        DateTimeOffset schedule,
        CloudEventTemplate<T> template,
        Dictionary<string, string>? labels = null,
        Dictionary<string, string>? annotations = null);

    Task ScheduleCronEvent<T>(
        string key,
        string schedule,
        CloudEventTemplate<T> template,
        DateTimeOffset? notBefore = null,
        DateTimeOffset? expirationTime = null,
        bool suspend = false,
        Dictionary<string, string>? labels = null,
        Dictionary<string, string>? annotations = null);
}

public class FabronClient : IFabronClient
{
    private readonly ILogger _logger;
    private readonly IClusterClient _client;
    private readonly FabronClientOptions _options;

    public FabronClient(ILogger<FabronClient> logger,
        IClusterClient client,
        IOptions<FabronClientOptions> options)
    {
        _logger = logger;
        _client = client;
        _options = options.Value;
    }

    public async Task ScheduleTimedEvent<T>(
        string key,
        DateTimeOffset schedule,
        CloudEventTemplate<T> template,
        Dictionary<string, string>? labels = null,
        Dictionary<string, string>? annotations = null)
    {
        var grain = _client.GetGrain<ITimedEventScheduler>(key);
        var spec = new TimedEventSpec
        {
            Schedule = schedule,
            CloudEventTemplate = JsonSerializer.Serialize(template, _options.JsonSerializerOptions),
        };
        await grain.Schedule(spec, labels, annotations, null);
    }

    public async Task ScheduleCronEvent<T>(
        string key,
        string schedule,
        CloudEventTemplate<T> template,
        DateTimeOffset? notBefore = null,
        DateTimeOffset? expirationTime = null,
        bool suspend = false,
        Dictionary<string, string>? labels = null,
        Dictionary<string, string>? annotations = null)
    {
        var grain = _client.GetGrain<ICronEventScheduler>(key);
        var spec = new CronEventSpec
        {
            Schedule = schedule,
            CloudEventTemplate = JsonSerializer.Serialize(template, _options.JsonSerializerOptions),
            NotBefore = notBefore,
            ExpirationTime = expirationTime,
            Suspend = suspend,
        };
        await grain.Schedule(spec, labels, annotations, null);
    }

}
