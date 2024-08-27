using CaptaTrader.Data.Base;

namespace CaptaTrader.Business.Entities
{
    [Serializable]
    public class AppCredentials : BaseEntity
    {
        public long UserId { get; set; }
        public required byte[] LoginSalt { get; set; }
        public required byte[] LoginHash { get; set; }
        public required int LoginWorkFactor { get; set; }
    }
}
