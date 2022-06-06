using FabronService.Resources.TimedEvents;
using Microsoft.AspNetCore.Builder;

namespace Microsoft.AspNetCore.Routing;

public static partial class Routes
{
    public static IEndpointRouteBuilder MapTimedEvents(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPut("/timed-events/{name}", TimedEventsHandler.Schedule)
            .WithName("TimedEvents_Put")
            .RequireAuthorization();

        endpoints.MapGet("/timed-events/{name}", TimedEventsHandler.Get)
            .WithName("TimedEvents_Get")
            .RequireAuthorization();

        return endpoints;
    }
}
