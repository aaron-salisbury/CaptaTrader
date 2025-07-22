using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CaptaTrader.Data.Entities;

[Index(nameof(UserName), IsUnique = true)]
public class EndUser
{
    [Key]
    public int EndUserId { get; set; }

    [Required]
    public required string UserName { get; set; }

    public UserCredential? UserCredential { get; set; }

    public UserConfig? UserConfig { get; set; }

    public IEnumerable<TradeOrder>? TradeOrders { get; set; }
}
