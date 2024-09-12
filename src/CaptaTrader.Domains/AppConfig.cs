using CaptaTrader.Domains.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CaptaTrader.Domains;

public class AppConfig : Model
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
    public int AppConfigId { get; set; }

    [Required, ForeignKey(nameof(EndUser))]
    public int EndUserId { get; set; }

    public EndUser? EndUser { get; set; }

    [EmailAddress, Display(Name = "E-Mail")]
    public string? Email { get; set; }

    public MarketDataAPIs? MarketDataAPI { get; set; }

    public string? MarketDataAPIKey { get; set; }

    public ExchangeAPIs? ExchangeAPI { get; set; }

    public string? ExchangeAPIKey { get; set; }

    public string? ExchangeSecretKey { get; set; }
}
