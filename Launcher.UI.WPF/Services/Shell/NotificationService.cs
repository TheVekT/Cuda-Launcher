using System.Collections.ObjectModel;
using Launcher.UI.WPF.Helpers.Enums;
using Launcher.UI.WPF.Models.Shell;
using Launcher.UI.WPF.Services.Shell.Abstractions;
using Launcher.UI.WPF.Services.Windows.Abstractions;

namespace Launcher.UI.WPF.Services.Shell;

public class NotificationService(IDispatcherService dispatcherService) : INotificationService
{
    public ObservableCollection<NotificationMessage> Notifications { get; } = new();

    public void Show(LocalizableText title, LocalizableText message, NotificationType type)
    {
        var notification = new NotificationMessage(title, message, type);

        dispatcherService.InvokeAsync(() =>
        {
            Notifications.Add(notification);
        });
    }
    
    public void ShowSuccess(LocalizableText title, LocalizableText message) => 
        Show(title, message, NotificationType.Success);

    public void ShowError(LocalizableText title, LocalizableText message) => 
        Show(title, message, NotificationType.Error);

    public void ShowWarning(LocalizableText title, LocalizableText message) => 
        Show(title, message, NotificationType.Warning);

    public void ShowInfo(LocalizableText title, LocalizableText message) => 
        Show(title, message, NotificationType.Info);

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
