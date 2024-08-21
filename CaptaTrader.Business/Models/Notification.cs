using System;

namespace CaptaTrader.Business.Models
{
    public class Notification
    {
        public enum NotificationTypes
        {
            Message,
            Table
        }

        public NotificationTypes NotificationType { get; set; }
        public string? Title { get; set; }
        public string? Message { get; set; }
        public Dictionary<string, string>? CellContentsByColumnNames { get; set; }
    }
}
