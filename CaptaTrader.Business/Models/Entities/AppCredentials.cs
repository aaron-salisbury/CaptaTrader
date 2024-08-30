using CaptaTrader.Business.Base;
using System.ComponentModel.DataAnnotations;

namespace CaptaTrader.Business.Models
{
    public class AppCredentials : IEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public long UserId { get; set; }

        [Required]
        public required byte[] LoginSalt { get; set; }

        [Required]
        public required byte[] LoginHash { get; set; }

        [Required]
        public required int LoginWorkFactor { get; set; }
    }
}
