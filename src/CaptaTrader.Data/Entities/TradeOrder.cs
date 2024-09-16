using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CaptaTrader.Data.Entities;

public class TradeOrder
{
    [Key]
    public int TradeOrderId { get; set; }

    [Required, ForeignKey(nameof(EndUser))]
    public int EndUserId { get; set; }

    public EndUser? EndUser { get; set; }

    [Required]
    public required DateTime TimeStamp { get; set; }

    [Required]
    public required int TransactionType { get; set; }

    [Required]
    public required string Symbol { get; set; }

    [Required]
    public required decimal PriceUSDT { get; set; }

    [Required]
    public required decimal Amount { get; set; }
}
