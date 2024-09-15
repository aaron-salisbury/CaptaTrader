using System.ComponentModel.DataAnnotations;

namespace CaptaTrader.Data.Entities;

public class EndUser
{
    [Key]
    public int EndUserId { get; private set; }

    [Required, Display(Name = "User Name")]
    public required string UserName { get; set; }

    public UserCredential? UserCredential { get; set; }

    public UserConfig? UserConfig { get; set; }

    public IEnumerable<TradeOrder>? TradeOrders { get; set; }
}
