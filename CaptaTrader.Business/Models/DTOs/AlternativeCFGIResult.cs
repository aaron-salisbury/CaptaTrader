using System.Text.Json.Serialization;

namespace CaptaTrader.Business.Models
{
    [Serializable]
    public class AlternativeCFGIResult
    {
        public required string Name { get; set; }
        public required List<AlternativeCFGIResultDatum> Data { get; set; }
        public required AlternativeCFGIResultMetadata Metadata { get; set; }
    }

    [Serializable]
    public class AlternativeCFGIResultDatum
    {
        public required int Value { get; set; }

        [JsonPropertyName("value_classification")]
        public required string ValueClassification { get; set; }

        public required string Timestamp { get; set; }

        [JsonPropertyName("time_until_update")]
        public string? TimeUntilUpdate { get; set; }
    }

    [Serializable]
    public class AlternativeCFGIResultMetadata
    {
        public object? Error { get; set; }
    }
}
