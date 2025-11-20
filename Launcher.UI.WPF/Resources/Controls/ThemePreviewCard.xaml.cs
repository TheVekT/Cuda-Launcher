using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Launcher.UI.WPF.Models; // Äëÿ ThemeMetadata

namespace Launcher.UI.WPF.Resources.Controls
{
    public partial class ThemePreviewCard : UserControl
    {
        public ThemePreviewCard()
        {
            InitializeComponent();
        }
        
        public static readonly DependencyProperty ThemeSourceProperty =
            DependencyProperty.Register("ThemeSource", typeof(Uri), typeof(ThemePreviewCard), new PropertyMetadata(null, OnThemeSourceChanged));

        public Uri ThemeSource
        {
            get => (Uri)GetValue(ThemeSourceProperty);
            set => SetValue(ThemeSourceProperty, value);
        }
        public static readonly DependencyProperty IsSelectedProperty =
            DependencyProperty.Register("IsSelected", typeof(bool), typeof(ThemePreviewCard), new PropertyMetadata(false));

        public bool IsSelected
        {
            get => (bool)GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }
        
        public static readonly DependencyProperty P_AppBackgroundProperty = DP("P_AppBackground");
        public Brush P_AppBackground { get => (Brush)GetValue(P_AppBackgroundProperty); set => SetValue(P_AppBackgroundProperty, value); }

        public static readonly DependencyProperty P_BorderPrimaryProperty = DP("P_BorderPrimary");
        public Brush P_BorderPrimary { get => (Brush)GetValue(P_BorderPrimaryProperty); set => SetValue(P_BorderPrimaryProperty, value); }

        public static readonly DependencyProperty P_BackgroundSurfaceProperty = DP("P_BackgroundSurface");
        public Brush P_BackgroundSurface { get => (Brush)GetValue(P_BackgroundSurfaceProperty); set => SetValue(P_BackgroundSurfaceProperty, value); }

        public static readonly DependencyProperty P_BorderSecondaryProperty = DP("P_BorderSecondary");
        public Brush P_BorderSecondary { get => (Brush)GetValue(P_BorderSecondaryProperty); set => SetValue(P_BorderSecondaryProperty, value); }

        public static readonly DependencyProperty P_InputBackgroundProperty = DP("P_InputBackground");
        public Brush P_InputBackground { get => (Brush)GetValue(P_InputBackgroundProperty); set => SetValue(P_InputBackgroundProperty, value); }

        public static readonly DependencyProperty P_BackgroundAccentProperty = DP("P_BackgroundAccent");
        public Brush P_BackgroundAccent { get => (Brush)GetValue(P_BackgroundAccentProperty); set => SetValue(P_BackgroundAccentProperty, value); }

        public static readonly DependencyProperty P_ForegroundOnAccentProperty = DP("P_ForegroundOnAccent");
        public Brush P_ForegroundOnAccent { get => (Brush)GetValue(P_ForegroundOnAccentProperty); set => SetValue(P_ForegroundOnAccentProperty, value); }

        public static readonly DependencyProperty P_ForegroundPrimaryProperty = DP("P_ForegroundPrimary");
        public Brush P_ForegroundPrimary { get => (Brush)GetValue(P_ForegroundPrimaryProperty); set => SetValue(P_ForegroundPrimaryProperty, value); }

        public static readonly DependencyProperty P_ForegroundSecondaryProperty = DP("P_ForegroundSecondary");
        public Brush P_ForegroundSecondary { get => (Brush)GetValue(P_ForegroundSecondaryProperty); set => SetValue(P_ForegroundSecondaryProperty, value); }
        
        public static readonly DependencyProperty ThemeNameProperty = DependencyProperty.Register("ThemeName", typeof(string), typeof(ThemePreviewCard), new PropertyMetadata("Unknown"));
        public string ThemeName { get => (string)GetValue(ThemeNameProperty); set => SetValue(ThemeNameProperty, value); }

        public static readonly DependencyProperty ThemeAuthorProperty = DependencyProperty.Register("ThemeAuthor", typeof(string), typeof(ThemePreviewCard), new PropertyMetadata("Unknown"));
        public string ThemeAuthor { get => (string)GetValue(ThemeAuthorProperty); set => SetValue(ThemeAuthorProperty, value); }
        
        private static DependencyProperty DP(string name) => 
            DependencyProperty.Register(name, typeof(Brush), typeof(ThemePreviewCard), new PropertyMetadata(Brushes.Transparent));
        
        private static void OnThemeSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ThemePreviewCard card && e.NewValue is Uri uri)
            {
                try
                {
                    var dict = new ResourceDictionary { Source = uri };
                    
                    if (dict.Contains("ThemeInfo") && dict["ThemeInfo"] is ThemeMetadata meta)
                    {
                        card.ThemeName = meta.Name;
                        card.ThemeAuthor = meta.Author;
                    }

                    card.P_AppBackground = GetBrush(dict, "AppBackground");
                    card.P_BorderPrimary = GetBrush(dict, "BorderPrimary");
                    
                    card.P_BackgroundSurface = GetBrush(dict, "BackgroundSurface");
                    card.P_BorderSecondary = GetBrush(dict, "BorderSecondary");
                    
                    card.P_InputBackground = GetBrush(dict, "InputBackground");
                    
                    card.P_BackgroundAccent = GetBrush(dict, "BackgroundAccent");
                    card.P_ForegroundOnAccent = GetBrush(dict, "ForegroundOnAccent");
                    
                    card.P_ForegroundPrimary = GetBrush(dict, "ForegroundPrimary");
                    card.P_ForegroundSecondary = GetBrush(dict, "ForegroundSecondary");
                }
                catch
                {
                    card.ThemeName = "Load Error";
                }
            }
        }

        private static Brush GetBrush(ResourceDictionary dict, string key)
        {
            if (dict.Contains(key) && dict[key] is Color color)
            {
                var b = new SolidColorBrush(color);
                b.Freeze();
                return b;
            }
            return Brushes.Transparent; 
        }
    }
}