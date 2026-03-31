
using System.Collections.ObjectModel;
using Launcher.Core.Enums;
using Launcher.Core.Models;

namespace Launcher.Core.Services.UI;

public interface INotificationService
{
    ObservableCollection<NotificationMessage> Notifications { get; }

    void Show(string title, string message, NotificationType type, double durationSeconds = 5);
    void ShowSuccess(string title, string message, double durationSeconds = 5);
    void ShowError(string title, string message, double durationSeconds = 7); // Ошибки висят чуть дольше
    void ShowWarning(string title, string message, double durationSeconds = 6);
    void ShowInfo(string title, string message, double durationSeconds = 5);
    
    // Метод для удаления (будет вызываться из XAML после завершения анимации)
    void Remove(Guid id);
}

