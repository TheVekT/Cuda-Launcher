using System.Collections.ObjectModel;
using Launcher.UI.WPF.Helpers;
using Launcher.UI.WPF.Models;

namespace Launcher.UI.WPF.Services.Abstractions;

public interface INotificationService
{
    ObservableCollection<NotificationMessage> Notifications { get; }

    void Show(string title, string message, NotificationType type, double durationSeconds = 5);
    void ShowSuccess(string title, string message, double durationSeconds = 5);
    void ShowError(string title, string message, double durationSeconds = 7);
    void ShowWarning(string title, string message, double durationSeconds = 6);
    void ShowInfo(string title, string message, double durationSeconds = 5);
    
    void Remove(Guid id);
}

