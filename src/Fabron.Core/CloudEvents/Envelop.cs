using System;
using System.Text.Json;
using Fabron.Models;

namespace Fabron.Core.CloudEvents;
public static class EnvelopeExtensions
{
    public static CloudEventEnvelop ToCloudEvent(this CronEvent cron, DateTimeOffset timestamp, JsonSerializerOptions jsonSerializerOptions)
    {
        var template = JsonSerializer.Deserialize<CloudEventTemplate>(cron.Spec.CloudEventTemplate, jsonSerializerOptions)!;
        string id = $"{cron.Metadata.Key}-{timestamp.ToUnixTimeSeconds()}";
        string source = template.Source ?? "fabron.io/cronevents";
        string type = template.Type ?? "fabron.cronevents.fired";
        var envelop = new CloudEventEnvelop(
            Id: id,
            Source: source,
            Type: type,
            Data: template.Data,
            DataSchema: template.DataSchema,
            Subject: template.Subject,
            Time: timestamp
            );
        return envelop;
    }

    public static CloudEventEnvelop ToCloudEvent(this TimedEvent schedule, DateTimeOffset timestamp, JsonSerializerOptions jsonSerializerOptions)
    {
        var template = JsonSerializer.Deserialize<CloudEventTemplate>(schedule.Spec.CloudEventTemplate, jsonSerializerOptions)!;
        string id = $"{schedule.Metadata.Key}-{timestamp.ToUnixTimeSeconds()}";
        string source = template.Source ?? "fabron.io/timedevents";
        string type = template.Type ?? "fabron.timedevents.fired";
        var envelop = new CloudEventEnvelop(
            Id: id,
            Source: source,
            Type: type,
            Data: template.Data,
            DataSchema: template.DataSchema,
            Subject: template.Subject,
            Time: timestamp
            );
        return envelop;
    }
}
