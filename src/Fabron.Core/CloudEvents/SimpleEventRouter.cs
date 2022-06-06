using System;
using System.Threading.Tasks;
using Fabron.Models;
using Microsoft.Extensions.Options;

namespace Fabron.Core.CloudEvents;

public class SimpleEventRouter : IEventRouter
{
    private readonly SimpleEventRouterOptions _options;

    public SimpleEventRouter(IOptions<SimpleEventRouterOptions> options)
    {
        _options = options.Value;
    }

    public ValueTask DispatchAsync(ScheduleMetadata metadata, CloudEventEnvelop envelop)
    {
        foreach (var route in _options.Routes)
        {
            if (route.Matches(metadata, envelop))
            {
                return route.HandleAsync(metadata, envelop);
            }
        }
        return ValueTask.CompletedTask;
    }

    public bool Matches(ScheduleMetadata metadata, CloudEventEnvelop envelop)
        => true;

    public class Route
    {
        public Func<ScheduleMetadata, CloudEventEnvelop, bool> Matches { get; init; }
            = (metadata, envelop) => true;
        public Func<ScheduleMetadata, CloudEventEnvelop, ValueTask> HandleAsync { get; init; }
            = (metadata, envelop) => ValueTask.CompletedTask;
    }
}
