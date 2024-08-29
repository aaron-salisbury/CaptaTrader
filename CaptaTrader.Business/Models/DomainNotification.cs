using CaptaTrader.Business.DTOs;

namespace CaptaTrader.Business.Models
{
    public class DomainNotification
    {
        public enum NotificationTypes
        {
            Message,
            TradeTransaction
        }

        public NotificationTypes NotificationType { get; set; }
        public string? Message { get; set; }
        public Trade? Trade { get; set; }
    }
}
