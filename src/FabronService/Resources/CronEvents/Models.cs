using System;
using Fabron.Core.CloudEvents;

namespace FabronService.Resources.CronEvents.Models;

public record CronEvent<TData>
(
    string Name,
    string Schedule,
    CloudEventTemplate<TData> Template,
    DateTimeOffset? NotBefore,
    DateTimeOffset? ExpirationTime,
    bool Suspend
);
