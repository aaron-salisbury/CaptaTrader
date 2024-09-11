using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CaptaTrader.Models;

public class AppCredential : IEntity
{
    [Key]
    public int Id { get; set; }

    [Required, ForeignKey(nameof(EndUser))]
    public int EndUserId { get; set; }

    public EndUser? EndUser { get; set; }

    [Required]
    public required byte[] LoginSalt { get; set; }

    [Required]
    public required byte[] LoginHash { get; set; }

    [Required]
    public required int LoginWorkFactor { get; set; }
}
