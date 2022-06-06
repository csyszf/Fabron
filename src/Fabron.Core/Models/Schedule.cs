using System;
using System.Collections.Generic;
using Orleans;

namespace Fabron.Models;

[GenerateSerializer]
public class ScheduleMetadata
{
    [Id(0)]
    public string Key { get; set; } = default!;

    [Id(1)]
    public DateTimeOffset CreationTimestamp { get; set; }

    [Id(2)]
    public DateTimeOffset? DeletionTimestamp { get; set; }

    [Id(3)]
    public Dictionary<string, string>? Labels { get; set; }

    [Id(4)]
    public Dictionary<string, string>? Annotations { get; set; }

    [Id(5)]
    public string? Owner { get; set; }
}
