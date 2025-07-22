using System.Collections.Generic;

namespace CaptaTrader.Data.Base;

public record HTTPJsonRequest
{
    public string HttpMethod { get; init; } = "GET";
    public bool UseCompression { get; init; }
    public string? JsonContent { get; init; }
    public string? UserAgent { get; init; }
    public Dictionary<string, string>? RequestHeaders { get; init; }
}
