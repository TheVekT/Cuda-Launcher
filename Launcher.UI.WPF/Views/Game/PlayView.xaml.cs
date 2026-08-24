using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Launcher.UI.WPF.Services;
using Launcher.UI.WPF.Stores;
using Launcher.UI.WPF.ViewModels.Game;

namespace Launcher.UI.WPF.Views.Game;

public partial class PlayView : UserControl
{
    private AppStore? _appStore;

    public PlayView()
    {
        InitializeComponent();
        DataContextChanged += PlayView_DataContextChanged;
        Loaded += PlayView_Loaded;
        Unloaded += PlayView_Unloaded;
    }

    private void PlayView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_appStore != null)
        {
            _appStore.PropertyChanged -= OnAppStorePropertyChanged;
        }

        if (DataContext is PlayViewModel vm)
        {
            _appStore = vm.AppStore;
            if (_appStore != null)
            {
                _appStore.PropertyChanged += OnAppStorePropertyChanged;
            }
        }
        else
        {
            _appStore = null;
        }

        UpdatePlayButtonState();
    }

    private void PlayView_Loaded(object sender, RoutedEventArgs e)
    {
        if (LocalizationService.Instance != null)
        {
            LocalizationService.Instance.PropertyChanged += OnLocalizationChanged;
        }
        UpdatePlayButtonState();
    }

    private void PlayView_Unloaded(object sender, RoutedEventArgs e)
    {
        if (LocalizationService.Instance != null)
        {
            LocalizationService.Instance.PropertyChanged -= OnLocalizationChanged;
        }
        if (_appStore != null)
        {
            _appStore.PropertyChanged -= OnAppStorePropertyChanged;
        }
    }

    private void OnAppStorePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AppStore.IsGameRunning))
        {
            Dispatcher.Invoke(UpdatePlayButtonState);
        }
    }

    private void OnLocalizationChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "Item[]")
        {
            Dispatcher.Invoke(UpdatePlayButtonState);
        }
    }

    private void UpdatePlayButtonState()
    {
        bool isRunning = _appStore?.IsGameRunning ?? false;

        if (PlayButton != null)
        {
            if (LocalizationService.Instance != null)
            {
                PlayButton.Content = LocalizationService.Instance[isRunning ? "Play.PlayButton.Close" : "Play.PlayButton"];
            }
            PlayButton.Tag = Application.Current.TryFindResource(isRunning ? "Icon.Close" : "Icon.Play");
        }
    }
}