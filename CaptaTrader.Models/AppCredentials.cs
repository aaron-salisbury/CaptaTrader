using System.ComponentModel.DataAnnotations;

namespace CaptaTrader.Models
{
    [Serializable]
    public class AppCredentials
    {
        [Key]
        public int AppCredentialsId { get; set; }
        public long UserId { get; set; }
        public required byte[] LoginSalt { get; set; }
        public required byte[] LoginHash { get; set; }
        public required int LoginWorkFactor { get; set; }
    }
}
