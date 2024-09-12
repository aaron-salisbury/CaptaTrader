using System.ComponentModel.DataAnnotations;

namespace CaptaTrader.Domains;

public class AppCredential
{
    [Key]
    public int AppCredentialId { get; private set; }

    [Required]
    public required byte[] LoginSalt { get; set; }

    [Required]
    public required byte[] LoginHash { get; set; }

    [Required]
    public required int LoginWorkFactor { get; set; }
}
