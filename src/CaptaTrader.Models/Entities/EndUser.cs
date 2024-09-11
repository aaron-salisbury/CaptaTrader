using System.ComponentModel.DataAnnotations;

namespace CaptaTrader.Models;

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
    public string UserName { get; set; }

    public AppCredential? AppCredential { get; set; }

    public IList<ExecutedTrade>? ExecutedTrades { get; set; }

    //TODO: Move these properties to a one-to-one relationship. Where they're used will need to start passing navigation paths.
    [EmailAddress, Display(Name = "E-Mail")]
    public string? Email { get; set; }

    public MarketDataAPIs? MarketDataAPI { get; set; }

    public string? MarketDataAPIKey { get; set; }

    public ExchangeAPIs? ExchangeAPI { get; set; }

    public string? ExchangeAPIKey { get; set; }

    public string? ExchangeSecretKey { get; set; }

    public EndUser(string userName)
    {
        UserName = userName;
    }
}
