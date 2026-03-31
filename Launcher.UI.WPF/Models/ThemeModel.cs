using System.Windows.Media.Imaging;

namespace Launcher.UI.WPF.Models;

public class ThemeModel
{
    public string Name { get; set; } = "Unknown";
    public string Author { get; set; } = "Unknown";
    
    public string ZipPath { get; set; } 
    public string XamlPath { get; set; } 
    public string BannerPath { get; set; } 
}