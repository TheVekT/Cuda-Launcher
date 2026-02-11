using System.Windows.Controls;
using Launcher.UI.WPF.Services;

namespace Launcher.UI.WPF.Resources.Overlay;

public partial class SettingsMenu : UserControl
{
    public SettingsMenu()
    {
        InitializeComponent();
    }

    private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox)
        {
            var selectedItem = comboBox.SelectedItem as ComboBoxItem;
            if (selectedItem != null)
            {
                var selectedLanguage = selectedItem.Content.ToString();
                if (!string.IsNullOrEmpty(selectedLanguage))
                {
                    string lanCode = LocalizationService.Instance.GetCodeByName(selectedLanguage);
                    Console.WriteLine("Selected language: " + selectedLanguage + ", code: " + lanCode);
                    LocalizationService.Instance.LoadLanguage(lanCode);
                }
            }
        }
    }
}