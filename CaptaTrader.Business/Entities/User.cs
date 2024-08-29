using CaptaTrader.Data.Base;
using System.ComponentModel.DataAnnotations;

namespace CaptaTrader.Business.Entities
{
    [Serializable]
    public class User : BaseEntity
    {
        [Required, Display(Name = "User Name")]
        public required string UserName { get; set; }

        [EmailAddress, Display(Name = "E-Mail")]
        public string? Email { get; set; }

        public string? CoinAPIKey { get; set; }

        public string? BinanceKey { get; set; }

        public string? BinanceSecretKey { get; set; }
    }
}
