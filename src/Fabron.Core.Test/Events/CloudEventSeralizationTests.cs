using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Fabron.Core.CloudEvents;
using FluentAssertions;
using Xunit;

namespace Fabron.Core.Test;

public class CloudEventSeralizationTests
{
    private readonly JsonSerializerOptions _option;

    public CloudEventSeralizationTests()
    {
        _option = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
    }

    [Fact]
    public void Serialize()
    {
        var cloudEvent = new CloudEventEnvelop(
            Id: "1",
            Source: "http://example.com",
            Type: "completed",
            Time: new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero),
            Data: JsonSerializer.Deserialize<JsonElement>(@"{""foo"":""bar""}", _option),
            DataSchema: null,
            Subject: null);

        string result = JsonSerializer.Serialize(cloudEvent, _option);
        string expected = @"{""id"":""1"",""source"":""http://example.com"",""type"":""completed"",""time"":""2020-01-01T00:00:00+00:00"",""data"":{""foo"":""bar""},""datacontenttype"":""application/json"",""specversion"":""1.0""}";
        result.Should().Be(expected);
    }
}
