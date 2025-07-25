using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CaptaTrader.Data.Entities;

public class CryptoAsset
{
    [Key]
    public int CryptoAssetId { get; set; }

    [Required, ForeignKey(nameof(EndUser))]
    public int EndUserId { get; set; }

    public EndUser? EndUser { get; set; }

    [Required]
    public required string Symbol { get; set; }

    [Required]
    public required decimal Amount { get; set; }

    [Required]
    public required decimal AveragePriceUSDT { get; set; }
}
