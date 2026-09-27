using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Launcher.Infrastructure.Customization.Models;
using Launcher.UI.WPF.Messages;
using Launcher.UI.WPF.Services.Customization;
using Launcher.UI.WPF.Services.Customization.Abstractions;
using Launcher.UI.WPF.Services.Windows.Abstractions;
using Launcher.UI.WPF.Stores;

namespace Launcher.UI.WPF.ViewModels.Config;

public partial class SettingsViewModel: ObservableObject
{
    private readonly SettingsStore _settingsStore;
    private readonly IThemeService _themeService;
    private readonly AppStore _appStore;
    private readonly IBrowserService _browserService;
    
    public SettingsStore SettingsStore => _settingsStore;
    public AppStore AppStore => _appStore;
    
    public SettingsViewModel(SettingsStore settingsStore, IThemeService themeService, AppStore appStore, IBrowserService browserService)
    {
        _settingsStore = settingsStore;
        _themeService = themeService;
        _appStore = appStore;
        _browserService = browserService;
    }

    private async Task ExecuteImportTheme(object _)
    {
        var openFileDialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Zip files (*.zip)|*.zip",
            Title = "Select a theme zip file"
        };
        if (openFileDialog.ShowDialog() == true)        {
            var selectedFile = openFileDialog.FileName;
            await _themeService.ImportTheme(selectedFile);
        }   
    }

    private async Task ExecuteImportLanguage(object _)
    {
        var openFileDialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "JSON files (*.json)|*.json",
            Title = "Select a localization json file"
        };
        if (openFileDialog.ShowDialog() == true)
        {
            var selectedFile = openFileDialog.FileName;
            
            await LocalizationService.Instance.ImportLocalization(selectedFile);
        }
    }
    
    //Commands
    [RelayCommand]
    private void CloseSelf() =>
        WeakReferenceMessenger.Default.Send(new CloseOverlayMessage());
    
    [RelayCommand]
    private void SelectTheme(object parameter)
    {
        if (parameter is ThemeModel theme)
        {
            _settingsStore.CurrentThemePath = theme.ZipPath;
        }
    }
    
    [RelayCommand]
    private async Task ImportTheme(object parameter) =>
        await ExecuteImportTheme(parameter);
    
    [RelayCommand]
    private async Task ImportLanguage(object parameter) =>
        await ExecuteImportLanguage(parameter);

    [RelayCommand]
    private void OpenGitHub(string? url = null) =>
        _browserService.OpenUrl(!string.IsNullOrWhiteSpace(url) ? url : "https://github.com/TheVekT/Cuda-Launcher");
}