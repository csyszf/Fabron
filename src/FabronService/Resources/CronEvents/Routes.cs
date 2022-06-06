using FabronService.Resources.CronEvents;
using Microsoft.AspNetCore.Builder;

namespace Microsoft.AspNetCore.Routing;

public static partial class Routes
{
    public static IEndpointRouteBuilder MapCronEvents(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPut("/cron-events/{name}", CronEventsHandler.Schedule)
            .WithName("CronEvents_Put")
            .RequireAuthorization();

        endpoints.MapGet("/cron-events/{name}", CronEventsHandler.Get)
            .WithName("CronEvents_Get")
            .RequireAuthorization();

        return endpoints;
    }
}
