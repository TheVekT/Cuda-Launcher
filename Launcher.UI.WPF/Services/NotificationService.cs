using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Launcher.Core.Enums;
using Launcher.Core.Models;
using Launcher.Core.Services.UI;

namespace Launcher.UI.WPF.Services
{
    public class NotificationService : INotificationService
    {
        public ObservableCollection<NotificationMessage> Notifications { get; } = new();
        
        private static NotificationService _instance;
        public static NotificationService Instance => _instance ??= new NotificationService();

        public void Show(string title, string message, NotificationType type, double durationSeconds = 5)
        {
            var notification = new NotificationMessage
            {
                Title = title,
                Message = message,
                Type = type,
                DurationSeconds = durationSeconds
            };

            // Гарантируем, что добавление произойдет в главном UI-потоке
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Notifications.Add(notification);
            });
        }

        // Синтаксический сахар для быстрого вызова из любого места кода
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
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var item = Notifications.FirstOrDefault(n => n.Id == id);
                if (item != null)
                {
                    Notifications.Remove(item);
                }
            });
        }
    }
}