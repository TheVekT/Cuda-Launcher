using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using Launcher.UI.WPF.Models;

namespace Launcher.UI.WPF.Views.Config.Components
{
    public partial class ThemePreviewCard : UserControl
    {
        public ThemePreviewCard()
        {
            InitializeComponent();
        }
        
        
        // === 1. ПУТЬ К РАСПАКОВАННОМУ XAML (Для отрисовки превью) ===
        public static readonly DependencyProperty ThemeXamlPathProperty =
            DependencyProperty.Register("ThemeXamlPath", typeof(string), typeof(ThemePreviewCard), new PropertyMetadata(null, OnThemeXamlPathChanged));

        public string ThemeXamlPath
        {
            get => (string)GetValue(ThemeXamlPathProperty);
            set => SetValue(ThemeXamlPathProperty, value);
        }

        // === 2. ПУТЬ К ZIP АРХИВУ (Для сохранения в настройки при клике) ===
        public static readonly DependencyProperty ZipPathSourceProperty =
            DependencyProperty.Register("ZipPathSource", typeof(string), typeof(ThemePreviewCard), new PropertyMetadata(null));
        
        public string ZipPathSource
        {
            get => (string)GetValue(ZipPathSourceProperty);
            set => SetValue(ZipPathSourceProperty, value);
        }

        // === 3. ТЕКУЩАЯ ВЫБРАННАЯ ТЕМА (Для подсветки) ===
        public static readonly DependencyProperty CurrentThemePathVMProperty =
            DependencyProperty.Register("CurrentThemePathVM", typeof(string), typeof(ThemePreviewCard), 
                new PropertyMetadata(null, OnCurrentThemePathVMChanged));
        
        public string CurrentThemePathVM
        {
            get => (string)GetValue(CurrentThemePathVMProperty);
            set => SetValue(CurrentThemePathVMProperty, value);
        }

        public static readonly DependencyProperty IsSelectedProperty =
           DependencyProperty.Register("IsSelected", typeof(bool), typeof(ThemePreviewCard), new PropertyMetadata(false));
        public bool IsSelected
        {
            get => (bool)GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }

        // Логика подсветки
        private static void OnCurrentThemePathVMChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ThemePreviewCard card)
            {
                // Сравниваем ZipPathSource (путь карточки) с CurrentThemePathVM (выбранная в сторе)
                card.IsSelected = !string.IsNullOrEmpty(card.ZipPathSource) && 
                                  !string.IsNullOrEmpty(card.CurrentThemePathVM) &&
                                  card.ZipPathSource == card.CurrentThemePathVM;
            }
        }
        
        // === ЗАГРУЗКА РЕСУРСОВ ИЗ XAML ФАЙЛА ===
        private static void OnThemeXamlPathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ThemePreviewCard card && e.NewValue is string path && File.Exists(path))
            {
                try
                {
                    // 1. Читаем XAML как текст
                    string xamlContent = File.ReadAllText(path);

                    // 2. ПАТЧИМ NAMESPACE (Фикс ошибки ThemeMetaData)
                    string assemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
                    string oldNs = "clr-namespace:Launcher.UI.WPF.Models";
                    string newNs = $"clr-namespace:Launcher.UI.WPF.Models;assembly={assemblyName}";

                    // Если в файле нет assembly, добавляем
                    if (xamlContent.Contains(oldNs) && !xamlContent.Contains(oldNs + ";assembly="))
                    {
                        xamlContent = xamlContent.Replace(oldNs, newNs);
                    }

                    // 3. Загружаем из памяти (MemoryStream)
                    using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xamlContent)))
                    {
                        var parserContext = new ParserContext();
                        parserContext.XmlnsDictionary.Add("", "http://schemas.microsoft.com/winfx/2006/xaml/presentation");
                        parserContext.XmlnsDictionary.Add("x", "http://schemas.microsoft.com/winfx/2006/xaml");
                        parserContext.XmlnsDictionary.Add("metadata", $"clr-namespace:Launcher.UI.WPF.Models;assembly={assemblyName}");

                        var dict = (ResourceDictionary)XamlReader.Load(stream, parserContext);

                        // 4. Извлекаем данные для карточки
                        if (dict.Contains("ThemeInfo") && dict["ThemeInfo"] is ThemeMetaData meta)
                        {
                            card.ThemeName = meta.Name;
                            card.ThemeAuthor = meta.Author;
                        }

                        card.P_GlobalFont = GetFont(dict, "GlobalFont");
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
                }
                catch
                {
                    card.ThemeName = "Load Error";
                }
            }
        }

        // Вспомогательные методы
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
        private static FontFamily GetFont(ResourceDictionary dict, string key)
        {
            if (dict.Contains(key) && dict[key] is FontFamily font)
            {
                return font;
            }
            return new FontFamily("Arial");
        }

        // === Dependency Properties для цветов (оставляем как было) ===
        public static readonly DependencyProperty P_GlobalFontProperty = DP_Font("P_GlobalFont");
        public FontFamily P_GlobalFont { get => (FontFamily)GetValue(P_GlobalFontProperty); set => SetValue(P_GlobalFontProperty, value); }
        
        public static readonly DependencyProperty P_AppBackgroundProperty = DP("P_AppBackground");
        public Brush P_AppBackground { get => (Brush)GetValue(P_AppBackgroundProperty); set => SetValue(P_AppBackgroundProperty, value); }
        // ... (остальные свойства такие же, как у тебя были, просто сократил для ответа)
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

        private static DependencyProperty DP(string name) => DependencyProperty.Register(name, typeof(Brush), typeof(ThemePreviewCard), new PropertyMetadata(Brushes.Transparent));
        private static DependencyProperty DP_Font(string name) => DependencyProperty.Register(name, typeof(FontFamily), typeof(ThemePreviewCard), new PropertyMetadata(new FontFamily("Arial")));
    }
}