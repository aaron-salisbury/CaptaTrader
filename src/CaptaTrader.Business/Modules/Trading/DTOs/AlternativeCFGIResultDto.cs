using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CaptaTrader.Business.Modules.Trading.DTOs;

public record AlternativeCFGIResultDto
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("data")]
    public required List<AlternativeCFGIResultDatum> Data { get; init; }

    [JsonPropertyName("metadata")]
    public AlternativeCFGIResultMetaData? MetaData { get; init; }
}

public record AlternativeCFGIResultDatum
{
    [JsonPropertyName("value")]
    public required string Value { get; init; }

    [JsonPropertyName("value_classification")]
    public string? ValueClassification { get; init; }

    [JsonPropertyName("timestamp")]
    public string? TimeStamp { get; init; }

    [JsonPropertyName("time_until_update")]
    public string? TimeUntilUpdate { get; init; }
}

public record AlternativeCFGIResultMetaData
{
    [JsonPropertyName("error")]
    public object? Error { get; init; }
}
