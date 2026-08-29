using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Launcher.Core.Config.Abstractions;
using Launcher.UI.WPF.Messages;
using Launcher.UI.WPF.Services.Customization.Abstractions;
using Launcher.UI.WPF.ViewModels.Game;

namespace Launcher.UI.WPF.Stores;

public partial class AppStore: ObservableObject, IRecipient<ThemeChangedMessage>
{
    private readonly IThemeService _themeService;
    private readonly ILauncherPathsService _pathsService;
    
    [ObservableProperty]
    private object? _currentOverlayView;
    [ObservableProperty]
    private bool _isOverlayVisible;
    private string? _themeBannerPath;
    [ObservableProperty]
    private bool _isLoading;
    [ObservableProperty]
    private bool _isGameRunning;
    [ObservableProperty]
    private bool _isDragDropActive;
    [ObservableProperty]
    private object? _currentView;
    [ObservableProperty]
    private bool _showCompactPlayButton;
    [ObservableProperty]
    private double _loadingProgress;
    [ObservableProperty]
    private string? _loadingStatusText = "Initiating...";
    [ObservableProperty]
    private string _loadingPercentText = "0%";

    partial void OnLoadingProgressChanged(double value)
    {
        LoadingPercentText = $"{value:0}%";
    }

    partial void OnIsLoadingChanged(bool value)
    {
        OnPropertyChanged(nameof(IsCurrentInstanceProcessing));
    }

    partial void OnIsGameRunningChanged(bool value)
    {
        OnPropertyChanged(nameof(IsCurrentInstanceProcessing));
    }

    public AppStore(IThemeService themeService, ILauncherPathsService pathsService)
    {
        _themeService = themeService;
        _pathsService = pathsService;
        
        ThemeBannerPath = themeService.CurrentTheme?.BannerPath ?? string.Empty;
        
        WeakReferenceMessenger.Default.RegisterAll(this);
    }

    public void Receive(ThemeChangedMessage message)
    {
        var theme = _themeService.GetThemeByFileName(message.ThemeFileName);
        ThemeBannerPath = theme?.BannerPath ?? string.Empty;
    }

    //Getters and Setters
    public bool IsCurrentInstanceProcessing => IsGameRunning || IsLoading;

    public string? ThemeBannerPath
    {
        get => _themeBannerPath;
        set
        {
            string targetPath = string.IsNullOrEmpty(value) 
                ? Path.Combine(_pathsService.AssetsDirectory, "Images", "banner-default.jpg")
                : value;
            if (_themeBannerPath == targetPath) 
                return;
            _themeBannerPath = targetPath; 
        
            OnPropertyChanged(nameof(CurrentBannerPath));
            OnPropertyChanged();
        }
    }

    public string? CurrentBannerPath 
        => ThemeBannerPath;

    partial void OnCurrentOverlayViewChanged(object? value)
    {
        IsOverlayVisible = value != null;
    }
    
    partial void OnCurrentViewChanged(object? value)
    {
        ShowCompactPlayButton = !(value is PlayViewModel);
    }
}