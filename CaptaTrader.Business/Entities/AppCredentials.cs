using CaptaTrader.Data.Base;
using System.ComponentModel.DataAnnotations;

namespace CaptaTrader.Business.Entities
{
    [Serializable]
    public class AppCredentials : BaseEntity
    {
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
