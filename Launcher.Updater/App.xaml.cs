using System.Configuration;
using System.Data;
using System.Windows;

namespace Launcher.Updater;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        var updater = new MainWindow();
        updater.Show();
    }

}