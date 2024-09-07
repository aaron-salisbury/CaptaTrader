using System;

namespace CaptaTrader.Business.Contracts
{
    public record Ping()
    {
        public string Value { get; init; }
    }
}
