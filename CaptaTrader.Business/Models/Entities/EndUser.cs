using CaptaTrader.Business.Base;
using System.ComponentModel.DataAnnotations;

namespace CaptaTrader.Business.Models
{
    public class EndUser : Model, IEntity
    {
        //TODO: Add option to select which APIs to use and logic set their keys after doing so.
        public enum MarketDataAPIs
        {
            CoinAPI
        }

        public enum ExchangeAPIs
        {
            Binance
        }

        [Key]
        public int Id { get; set; }

        [Required, Display(Name = "User Name")]
        public required string UserName { get; set; }

        [EmailAddress, Display(Name = "E-Mail")]
        public string? Email { get; set; }

        public MarketDataAPIs? MarketDataAPI { get; set; }

        public string? MarketDataAPIKey { get; set; }

        public ExchangeAPIs? ExchangeAPI { get; set; }

        public string? ExchangeAPIKey { get; set; }

        public string? ExchangeSecretKey { get; set; }
    }
}
