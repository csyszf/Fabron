
using System;
using Orleans;

namespace Fabron.Models;

[GenerateSerializer]
public class CronEvent
{
    [Id(0)]
    public ScheduleMetadata Metadata { get; set; } = default!;

    [Id(1)]
    public CronEventSpec Spec { get; set; } = default!;
};

[GenerateSerializer]
public class CronEventSpec
{

    [Id(0)]
    public string Schedule { get; init; } = default!;

    [Id(1)]
    public string CloudEventTemplate { get; init; } = default!;

    [Id(2)]
    public DateTimeOffset? NotBefore { get; set; }

    [Id(3)]
    public DateTimeOffset? ExpirationTime { get; set; }

    [Id(4)]
    public bool Suspend { get; set; }
}

