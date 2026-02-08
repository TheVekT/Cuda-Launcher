using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Launcher.UI.WPF.Resources.Pages;
using Launcher.UI.WPF.ViewModels;

namespace Launcher.UI.WPF;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
    
    private void NavRadio_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb && rb.Tag is string tag)
        {
            try
            {
                MainFrame.Source = new System.Uri(tag, System.UriKind.Relative);
            }
            catch
            {
                throw new NotImplementedException();
            }
        }
    }

    private void MainFrame_Navigated(object sender, NavigationEventArgs e)
    {
        if (DataContext is MainViewModel mainVM)
        {
            bool isPlayPage = e.Uri != null && e.Uri.OriginalString.Contains("Play.xaml", StringComparison.OrdinalIgnoreCase);
            
            mainVM.ShowCompactPlayButton = !isPlayPage;
            
            if (e.Content is Installations installationsPage)
            {
                installationsPage.DataContext = mainVM.InstallationsVM;
            }
        }
    }

}