using System.Collections.ObjectModel;
using Launcher.UI.WPF.Helpers.Enums;
using Launcher.UI.WPF.Models.Shell;

namespace Launcher.UI.WPF.Services.Shell.Abstractions;

public interface INotificationService
{
    ObservableCollection<NotificationMessage> Notifications { get; }

    void Show(LocalizableText title, LocalizableText message, NotificationType type);
    void ShowSuccess(LocalizableText title, LocalizableText message);
    void ShowError(LocalizableText title, LocalizableText message);
    void ShowWarning(LocalizableText title, LocalizableText message);
    void ShowInfo(LocalizableText title, LocalizableText message);
    
    void Remove(Guid id);
}

