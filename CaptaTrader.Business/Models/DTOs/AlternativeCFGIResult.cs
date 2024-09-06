using System.Text.Json.Serialization;

namespace CaptaTrader.Business.Models
{
    [Serializable]
    public class AlternativeCFGIResult
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("data")]
        public required List<AlternativeCFGIResultDatum> Data { get; set; }

        [JsonPropertyName("metadata")]
        public AlternativeCFGIResultMetadata? Metadata { get; set; }
    }

    [Serializable]
    public class AlternativeCFGIResultDatum
    {
        [JsonPropertyName("value")]
        public required string Value { get; set; }

        [JsonPropertyName("value_classification")]
        public string? ValueClassification { get; set; }

        [JsonPropertyName("timestamp")]
        public string? Timestamp { get; set; }

        [JsonPropertyName("time_until_update")]
        public string? TimeUntilUpdate { get; set; }
    }

    [Serializable]
    public class AlternativeCFGIResultMetadata
    {
        [JsonPropertyName("error")]
        public object? Error { get; set; }
    }
}
