using System.Collections.ObjectModel;
using Launcher.UI.WPF.Helpers.Enums;
using Launcher.UI.WPF.Models.Shell;
using Launcher.UI.WPF.Services.Shell.Abstractions;
using Launcher.UI.WPF.Services.Windows.Abstractions;

namespace Launcher.UI.WPF.Services.Shell;

public class NotificationService(IDispatcherService dispatcherService) : INotificationService
{
    public ObservableCollection<NotificationMessage> Notifications { get; } = new();

    public void Show(string title, string message, NotificationType type, double durationSeconds = 5)
    {
        var notification = new NotificationMessage
        {
            Title = title,
            Message = message,
            Type = type,
            DurationSeconds = durationSeconds
        };
        
        dispatcherService.InvokeAsync(() =>
        {
            Notifications.Add(notification);
        });
    }
    
    public void ShowSuccess(string title, string message, double durationSeconds = 5) => 
        Show(title, message, NotificationType.Success, durationSeconds);

    public void ShowError(string title, string message, double durationSeconds = 7) => 
        Show(title, message, NotificationType.Error, durationSeconds);

    public void ShowWarning(string title, string message, double durationSeconds = 6) => 
        Show(title, message, NotificationType.Warning, durationSeconds);

    public void ShowInfo(string title, string message, double durationSeconds = 5) => 
        Show(title, message, NotificationType.Info, durationSeconds);

    public void Remove(Guid id)
    {
        dispatcherService.InvokeAsync(() =>
        {
            var item = Notifications.FirstOrDefault(n => n.Id == id);
            if (item != null)
            {
                Notifications.Remove(item);
            }
        });
    }
}
