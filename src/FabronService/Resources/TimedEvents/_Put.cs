using System;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Fabron;
using Fabron.Core.CloudEvents;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FabronService.Resources.TimedEvents;

public static partial class TimedEventsHandler
{
    public static async Task<IResult> Schedule(
        [FromRoute] string name,
        [FromBody] ScheduleTimedEventRequest req,
        ClaimsPrincipal user,
        [FromServices] IFabronClient fabronClient)
    {
        string? tenant = user.Identity?.Name;
        if (string.IsNullOrEmpty(tenant))
            return Results.Unauthorized();

        string key = $"{tenant}/{name}";
        await fabronClient.ScheduleTimedEvent(
            key,
            req.Schedule,
            req.Template);

        return Results.CreatedAtRoute("TimedEvents_Get", new { name });
    }
}

public record ScheduleTimedEventRequest
(
    DateTimeOffset Schedule,
    CloudEventTemplate<JsonElement> Template
);
