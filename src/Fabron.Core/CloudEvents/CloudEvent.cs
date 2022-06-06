using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fabron.Core.CloudEvents;

public record CloudEventTemplate(
    [property: JsonPropertyName("data")]
    JsonElement Data,
    [property: JsonPropertyName("type")]
    string? Type = null,
    [property: JsonPropertyName("source")]
    string? Source = null,
    [property: JsonPropertyName("subject")]
    string? Subject = null,
    [property: JsonPropertyName("dataschema")]
    Uri? DataSchema = null
);

public record CloudEventTemplate<T>(
    [property: JsonPropertyName("data")]
    T Data,
    [property: JsonPropertyName("type")]
    string? Type = null,
    [property: JsonPropertyName("source")]
    string? Source = null,
    [property: JsonPropertyName("subject")]
    string? Subject = null,
    [property: JsonPropertyName("dataschema")]
    Uri? DataSchema = null
);

public record CloudEventEnvelop(
    [property: JsonPropertyName("id")]
    string Id,
    [property: JsonPropertyName("source")]
    string Source,
    [property: JsonPropertyName("type")]
    string Type,
    [property: JsonPropertyName("time")]
    DateTimeOffset Time,
    [property: JsonPropertyName("data")]
    JsonElement Data,
    [property: JsonPropertyName("dataschema")]
    Uri? DataSchema,
    [property: JsonPropertyName("subject")]
    string? Subject
)
{
    [JsonPropertyName("datacontenttype")]
    public string DataContentType { get; } = "application/json";

    [JsonPropertyName("specversion")]
    public string SpecVersion { get; } = "1.0";
};
