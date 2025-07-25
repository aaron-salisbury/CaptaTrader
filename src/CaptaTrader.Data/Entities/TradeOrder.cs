using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CaptaTrader.Data.Entities;

[Index(nameof(EndUserId))]
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
    public required int TransactionType { get; set; } // 0 = Buy, 1 = Sell

    [Required]
    public required string AssetSymbolBuying { get; set; }

    [Required]
    public required string AssetSymbolSpending { get; set; }

    [Required]
    public required decimal Price { get; set; }

    [Required]
    public required decimal Amount { get; set; }
}
