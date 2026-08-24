using System.Collections.ObjectModel;
using Launcher.Core.Common.Enums;
using Launcher.Core.Common.Models;
using Launcher.Core.UI.Abstractions;
using Launcher.UI.WPF.Services.Abstractions;

namespace Launcher.UI.WPF.Services;

public class NotificationService : INotificationService
{
    private readonly IDispatcherService _dispatcherService;
    
    public ObservableCollection<NotificationMessage> Notifications { get; } = new();
    
    public NotificationService(IDispatcherService dispatcherService)
    {
        _dispatcherService = dispatcherService;
    }

    public void Show(string title, string message, NotificationType type, double durationSeconds = 5)
    {
        var notification = new NotificationMessage
        {
            Title = title,
            Message = message,
            Type = type,
            DurationSeconds = durationSeconds
        };
        
        _dispatcherService.InvokeAsync(() =>
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
        _dispatcherService.InvokeAsync(() =>
        {
            var item = Notifications.FirstOrDefault(n => n.Id == id);
            if (item != null)
            {
                Notifications.Remove(item);
            }
        });
    }
}
