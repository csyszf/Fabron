using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Fabron;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FabronService.Resources.TimedEvents;

public static partial class TimedEventsHandler
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
        var timedEvent = await fabronClient.GetTimedEvent<JsonElement>(key);
        if (timedEvent is null)
            return Results.NotFound();
        return Results.Ok(timedEvent);
    }
}
