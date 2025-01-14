using System;
using System.Threading.Tasks;
using Fabron.CloudEvents;
using Fabron.Models;
using Microsoft.Toolkit.Diagnostics;

namespace FabronService.EventRouters;

public class AnnotationBasedEventRouter : IEventRouter
{
    private readonly IHttpDestinationHandler _http;

    public AnnotationBasedEventRouter(IHttpDestinationHandler http)
    {
        _http = http;
    }

    public bool Matches(ScheduleMetadata metadata, CloudEventEnvelop envelop)
    {
        var annotations = metadata.Annotations;
        return annotations is not null && annotations.ContainsKey("routing.fabron.io/destination");
    }

    public Task DispatchAsync(ScheduleMetadata metadata, CloudEventEnvelop envelop)
    {
        string? destination = metadata.Annotations?["routing.fabron.io/destination"];
        Guard.IsNotNull(destination, nameof(destination));
        if (destination.StartsWith("http"))
        {
            return _http.SendAsync(new Uri(destination), envelop);
        }
        return ThrowHelper.ThrowArgumentOutOfRangeException<Task>(nameof(destination));
    }
}

