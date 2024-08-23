using System.ComponentModel.DataAnnotations;

namespace CaptaTrader.Models
{
    public class User
    {
        [Key]
        public long UserId { get; set; }
        public required string Username { get; set; }
        public string? Email { get; set; }
        public string? CoinAPIKey { get; set; }
        public string? BinanceKey { get; set; }
        public string? BinanceSecretKey { get; set; }
    }
}
