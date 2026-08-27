using System.Windows;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.Messaging;
using Launcher.Core.Common.Models;
using Launcher.UI.WPF.Messages;
using Launcher.UI.WPF.Models;

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
                WeakReferenceMessenger.Default.Send(new CloseNotificationMessage(message.Id));
            }
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is NotificationMessage message)
                WeakReferenceMessenger.Default.Send(new CloseNotificationMessage(message.Id));
        }
    }
}