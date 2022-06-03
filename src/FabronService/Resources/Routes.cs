using FabronService.Resources.CronHttpReminders;
using Microsoft.AspNetCore.Builder;

namespace Microsoft.AspNetCore.Routing;

public static class Routes
{
    const string CronHttpReminders = nameof(FabronService.Resources.CronHttpReminders);
    internal const string CronHttpReminders_Get = $"{CronHttpReminders}_{nameof(CronHttpRemindersHandler.Get)}";
    internal const string CronHttpReminders_GetSchedules = $"{CronHttpReminders}_{nameof(CronHttpRemindersHandler.GetSchedules)}";
    internal const string CronHttpReminders_Register = $"{CronHttpReminders}_{nameof(CronHttpRemindersHandler.Register)}";
    public static IEndpointRouteBuilder MapCronHttpReminders(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPut("/cron-http-reminders/{name}", CronHttpRemindersHandler.Register)
            .WithName(CronHttpReminders_Register)
            .RequireAuthorization();
        endpoints.MapGet("/cron-http-reminders/{name}", CronHttpRemindersHandler.Get)
            .WithName(CronHttpReminders_Get)
            .RequireAuthorization();
        endpoints.MapGet("/cron-http-reminders/{name}/schedules", CronHttpRemindersHandler.GetSchedules)
            .WithName(CronHttpReminders_GetSchedules)
            .RequireAuthorization();

        return endpoints;
    }

    const string HttpReminders_Base = nameof(FabronService.Resources.HttpReminders);
    internal const string HttpReminders_Get = $"{HttpReminders_Base}_{nameof(HttpRemindersHandler.Get)}";
    internal const string HttpReminders_Register = $"{HttpReminders_Base}_{nameof(HttpRemindersHandler.Register)}";
    public static IEndpointRouteBuilder MapHttpReminders(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPut("/http-reminders/{name}", HttpRemindersHandler.Register)
            .WithName(HttpReminders_Register)
            .RequireAuthorization();
        endpoints.MapGet("/http-reminders/{name}", HttpRemindersHandler.Get)
            .WithName(HttpReminders_Get)
            .RequireAuthorization();

        return endpoints;
    }
}
