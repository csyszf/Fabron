
using System;
using System.Collections.Generic;
using Orleans;

namespace Fabron.Models;

[GenerateSerializer]
public class CronSchedule
{
    [Id(0)]
    public ScheduleMetadata Metadata { get; set; } = default!;

    [Id(1)]
    public CronScheduleSpec Spec { get; set; } = default!;
};

[GenerateSerializer]
public class CronScheduleSpec
{

    [Id(0)]
    public string Schedule { get; init; } = default!;

    [Id(1)]
    public string Event { get; init; } = default!;

    [Id(2)]
    public DateTimeOffset? NotBefore { get; set; }

    [Id(3)]
    public DateTimeOffset? ExpirationTime { get; set; }

    [Id(4)]
    public bool Suspend { get; set; }
}

