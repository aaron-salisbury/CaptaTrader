using System.Text.Json.Serialization;

namespace CaptaTrader.Models;

[Serializable]
public record AlternativeCFGIResult
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("data")]
    public required List<AlternativeCFGIResultDatum> Data { get; set; }

    [JsonPropertyName("metadata")]
    public AlternativeCFGIResultMetaData? MetaData { get; set; }
}

[Serializable]
public record AlternativeCFGIResultDatum
{
    [JsonPropertyName("value")]
    public required string Value { get; set; }

    [JsonPropertyName("value_classification")]
    public string? ValueClassification { get; set; }

    [JsonPropertyName("timestamp")]
    public string? TimeStamp { get; set; }

    [JsonPropertyName("time_until_update")]
    public string? TimeUntilUpdate { get; set; }
}

[Serializable]
public record AlternativeCFGIResultMetaData
{
    [JsonPropertyName("error")]
    public object? Error { get; set; }
}
