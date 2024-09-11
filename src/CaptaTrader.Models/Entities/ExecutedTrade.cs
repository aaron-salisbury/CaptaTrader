using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CaptaTrader.Models;

public class ExecutedTrade : IEntity
{
    [Key]
    public int Id { get; set; }

    [Required, ForeignKey(nameof(EndUser))]
    public int EndUserId { get; set; }

    public EndUser? EndUser { get; set; }

    [Required, Display(Name = "Trade Order")]
    public required TradeOrder TradeOrder { get; set; }
}
