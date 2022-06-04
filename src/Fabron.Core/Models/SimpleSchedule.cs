
using System;
using Orleans;

namespace Fabron.Models;

[GenerateSerializer]
public class SimpleSchedule
{
    [Id(0)]
    public ScheduleMetadata Metadata { get; set; } = default!;

    [Id(1)]
    public SimpleScheduleSpec Spec { get; set; } = default!;
};

[GenerateSerializer]
public class SimpleScheduleSpec
{
    [Id(0)]
    public DateTimeOffset? Schedule { get; init; } = default!;

    [Id(1)]
    public string CloudEvent { get; init; } = default!;
}

