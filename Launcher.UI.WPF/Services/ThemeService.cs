using System;
using System.Linq;
using System.Windows;

namespace Launcher.UI.WPF.Services
{
    public class ThemeService
    {
        public void ChangeTheme(string themePath)
        {
            var dicts = Application.Current.Resources.MergedDictionaries;
            
            var oldTheme = dicts.FirstOrDefault(d => d.Contains("ThemeInfo"));
            var brushes = dicts.FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("Brushes.xaml"));
            if (oldTheme != null) dicts.Remove(oldTheme);
            if (brushes != null) dicts.Remove(brushes);
            dicts.Add(new ResourceDictionary { Source = new Uri(themePath, UriKind.Relative) });
            dicts.Add(new ResourceDictionary { Source = new Uri("Resources/Styles/Brushes.xaml", UriKind.Relative) });
        }
    }
}