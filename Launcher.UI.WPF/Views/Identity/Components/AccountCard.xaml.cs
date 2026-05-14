using System.Windows;
using System.Windows.Controls;

namespace Launcher.UI.WPF.Views.Identity.Components;

public partial class AccountCard : UserControl
{
    public AccountCard()
    {
        InitializeComponent();
    }
    private void OpenContextMenu(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.ContextMenu != null)
        {
            btn.ContextMenu.PlacementTarget = btn;
            
            btn.ContextMenu.IsOpen = true;
        }
    }
}