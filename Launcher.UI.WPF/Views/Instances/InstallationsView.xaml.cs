using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Launcher.UI.WPF.Services;
using Launcher.UI.WPF.Views.Instances.Components;

namespace Launcher.UI.WPF.Views.Instances;

public partial class InstallationsView : UserControl
{
    public InstallationsView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (LocalizationService.Instance != null)
            LocalizationService.Instance.PropertyChanged += OnLocalizationChanged;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (LocalizationService.Instance != null)
            LocalizationService.Instance.PropertyChanged -= OnLocalizationChanged;
    }

    private void OnLocalizationChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "Item[]")
            VersionElement.RefreshAllLastPlayed();
    }
}