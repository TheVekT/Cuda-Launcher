using System.Windows;
using System.Windows.Controls;
using Launcher.Core.Common.Models;
using Launcher.UI.WPF.Services;

namespace Launcher.UI.WPF.Views.Common.Notifications
{
    public partial class InfoNotify : UserControl
    {
        public InfoNotify()
        {
            InitializeComponent();
        }

        private void FadeOut_Completed(object sender, EventArgs e)
        {
            if (DataContext is NotificationMessage message)
            {
                NotificationService.Instance?.Remove(message.Id);
            }
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is NotificationMessage message)
            {
                // Мгновенно удаляем уведомление при клике на крестик
                NotificationService.Instance?.Remove(message.Id);
            }
        }
    }
}