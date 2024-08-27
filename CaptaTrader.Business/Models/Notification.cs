using CaptaTrader.Business.Entities;

namespace CaptaTrader.Business.Models
{
    public class Notification
    {
        public enum NotificationTypes
        {
            Message,
            Transaction
        }

        public NotificationTypes NotificationType { get; set; }
        public string? Message { get; set; }
        public TradeOrder? TradeOrder { get; set; }
    }
}
