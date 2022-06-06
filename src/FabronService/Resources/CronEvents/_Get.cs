using System;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Fabron;
using Fabron.Core.CloudEvents;
using FabronService.Resources.CronEvents.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FabronService.Resources.CronEvents;

public static partial class CronEventsHandler
{
    public static async Task<IResult> Get(
        [FromRoute] string name,
        ClaimsPrincipal user,
        [FromServices] IFabronClient fabronClient)
    {
        string? tenant = user.Identity?.Name;
        if (string.IsNullOrEmpty(tenant))
            return Results.Unauthorized();

        string key = $"{tenant}/{name}";
        var cronEvent = await fabronClient.GetCronEvent<JsonElement>(key);
        if (cronEvent is null)
            return Results.NotFound();

        var template = JsonSerializer.Deserialize<CloudEventTemplate<JsonElement>>(cronEvent.Spec.CloudEventTemplate)!;
        var result = new CronEvent<JsonElement>(
            name,
            cronEvent.Spec.Schedule,
            template,
            cronEvent.Spec.NotBefore,
            cronEvent.Spec.ExpirationTime,
            cronEvent.Spec.Suspend);
        return Results.Ok(result);
    }
}
