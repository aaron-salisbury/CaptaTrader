using CaptaTrader.Domains.Base;
using System.ComponentModel.DataAnnotations;

namespace CaptaTrader.Domains;

public class EndUser : Model
{
    [Key]
    public int EndUserId { get; set; }

    [Required, Display(Name = "User Name")]
    public required string UserName { get; set; }

    public AppCredential? AppCredential { get; set; }

    public AppConfig AppConfig { get; set; } = new AppConfig();

    public IEnumerable<ExecutedTrade>? ExecutedTrades { get; set; }
}
