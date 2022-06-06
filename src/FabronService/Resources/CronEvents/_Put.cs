using System;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Fabron;
using Fabron.Core.CloudEvents;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FabronService.Resources.CronEvents;

public static partial class CronEventsHandler
{
    public static async Task<IResult> Schedule(
        [FromRoute] string name,
        [FromBody] ScheduleCronEventRequest req,
        ClaimsPrincipal user,
        [FromServices] IFabronClient fabronClient)
    {
        string? tenant = user.Identity?.Name;
        if (string.IsNullOrEmpty(tenant))
            return Results.Unauthorized();

        string key = $"{tenant}/{name}";
        await fabronClient.ScheduleCronEvent(
            key,
            req.Schedule,
            req.Template,
            req.NotBefore,
            req.ExpirationTime,
            req.Suspend);

        return Results.CreatedAtRoute("CronEvents_Get", new { name });
    }
}

public record ScheduleCronEventRequest
(
    string Name,
    string Schedule,
    CloudEventTemplate<JsonElement> Template,
    DateTimeOffset? NotBefore,
    DateTimeOffset? ExpirationTime,
    bool Suspend
);
