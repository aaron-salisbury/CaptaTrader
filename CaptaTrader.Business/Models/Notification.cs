using System;

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
        public Dictionary<string, string>? TransactionValuesByColumns { get; set; }
    }
}
