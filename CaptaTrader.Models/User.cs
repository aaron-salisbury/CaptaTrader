using System.ComponentModel.DataAnnotations;

namespace CaptaTrader.Models
{
    [Serializable]
    public class User
    {
        [Key]
        public int UserId { get; set; }
        public required string UserName { get; set; }
        public string? Email { get; set; }
        public string? CoinAPIKey { get; set; }
        public string? BinanceKey { get; set; }
        public string? BinanceSecretKey { get; set; }
    }
}
