using System.ComponentModel;
using System.Windows;
using Launcher.UI.WPF.Services.Customization;
using Launcher.UI.WPF.Views.Instances.Components;

namespace Launcher.UI.WPF.Views.Instances;

public partial class InstallationsView
{
    public InstallationsView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        LocalizationService.Instance.PropertyChanged += OnLocalizationChanged;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        LocalizationService.Instance.PropertyChanged -= OnLocalizationChanged;
    }

    private void OnLocalizationChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "Item[]")
            VersionElement.RefreshAllLastPlayed();
    }
}