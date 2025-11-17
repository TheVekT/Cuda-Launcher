using System.Windows;
using System.Windows.Controls;

namespace Launcher.UI.WPF.Resources.Controls;

public partial class SkinCard : UserControl
{
    public SkinCard()
    {
        InitializeComponent();
    }
    
    public event RoutedEventHandler? ApplyClicked;


    public event RoutedEventHandler? DeleteClicked;

    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        ApplyClicked?.Invoke(this, e);
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        DeleteClicked?.Invoke(this, e);
    }
}

