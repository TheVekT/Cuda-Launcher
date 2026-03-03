
using Launcher.Core.Enums;

namespace Launcher.Core.Models
{
    public class NotificationMessage
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        public string Title { get; set; }
        public string Message { get; set; }
        public NotificationType Type { get; set; }
        
        public double DurationSeconds { get; set; } 
    }
}