using System.ComponentModel.DataAnnotations;

namespace CaptaTrader.Models
{
    public class User
    {
        [Key]
        public long UserId { get; set; }
        public required byte[] LoginSalt { get; set; }
        public required byte[] LoginHash { get; set; }
        public required int LoginWorkFactor { get; set; }
        public string? Email { get; set; }
        public string? CoinAPIKey { get; set; }
        public string? BinanceKey { get; set; }
        public string? BinanceSecretKey { get; set; }
    }
}
