using Launcher.UI.WPF.Helpers;
using Launcher.UI.WPF.Helpers.Enums;

namespace Launcher.UI.WPF.Models.Shell;

public class NotificationMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    
    public double DurationSeconds { get; set; } 
}
