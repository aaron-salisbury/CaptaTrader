using CaptaTrader.Data.Base;

namespace CaptaTrader.Business.Entities
{
    [Serializable]
    public class User : BaseEntity
    {
        public required string UserName { get; set; }
        public string? Email { get; set; }
        public string? CoinAPIKey { get; set; }
        public string? BinanceKey { get; set; }
        public string? BinanceSecretKey { get; set; }
    }
}
