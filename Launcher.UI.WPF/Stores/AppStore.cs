using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FluentResults;
using Launcher.Core.Config.Abstractions;
using Launcher.Infrastructure.Updates.Abstractions;
using Launcher.Infrastructure.Updates.Models;
using Launcher.UI.WPF.Messages;
using Launcher.UI.WPF.Services.Customization.Abstractions;
using Launcher.UI.WPF.Services.Windows.Abstractions;
using Launcher.UI.WPF.ViewModels.Game;

namespace Launcher.UI.WPF.Stores;

public partial class AppStore: ObservableObject, IRecipient<ThemeChangedMessage>
{
    private readonly IThemeService _themeService;
    private readonly ILauncherPathsService _pathsService;
    private readonly IAppVersionProvider _appVersionProvider;
    private readonly IUpdateCheckerService _updateCheckerService;
    private readonly IUpdateLauncherService _updateLauncherService;
    private readonly IApplicationLifetimeService _applicationLifetimeService;
    
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
    [ObservableProperty]
    private string _currentAppVersion = string.Empty;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAppUpdateAvailable))]
    private Result<UpdateCheckResult>? _updateCheckResult;

    public AppStore(IThemeService themeService, 
        ILauncherPathsService pathsService, 
        IAppVersionProvider appVersionProvider, 
        IUpdateCheckerService updateCheckerService, 
        IUpdateLauncherService updateLauncherService,
        IApplicationLifetimeService applicationLifetimeService)
    {
        _themeService = themeService;
        _pathsService = pathsService;
        _appVersionProvider = appVersionProvider;
        _updateCheckerService = updateCheckerService;
        _updateLauncherService = updateLauncherService;
        _applicationLifetimeService = applicationLifetimeService;
        
        CurrentAppVersion = _appVersionProvider.CurrentVersion;
        
        ThemeBannerPath = themeService.CurrentTheme?.BannerPath ?? string.Empty;
        
        WeakReferenceMessenger.Default.RegisterAll(this);
    }
    
    [RelayCommand]
    private async Task CheckForUpdatesAsync() =>
        UpdateCheckResult = await _updateCheckerService.CheckForUpdatesAsync(CurrentAppVersion, true);
    
    [RelayCommand]
    private void RestartAndUpdate()
    {
        if (UpdateCheckResult is null || !UpdateCheckResult.IsSuccess || UpdateCheckResult.Value is null)
            return;
        var launchResult = _updateLauncherService.LaunchUpdater(UpdateCheckResult.Value);
        if (launchResult.IsSuccess)
            _applicationLifetimeService.Shutdown();
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

    public string? CurrentBannerPath => ThemeBannerPath;
    
    public bool IsAppUpdateAvailable => UpdateCheckResult?.Value?.IsUpdateAvailable ?? false;

    partial void OnCurrentOverlayViewChanged(object? value) =>
        IsOverlayVisible = value != null;
    
    partial void OnCurrentViewChanged(object? value) =>
        ShowCompactPlayButton = !(value is PlayViewModel);
    
    partial void OnLoadingProgressChanged(double value) =>
        LoadingPercentText = $"{value:0}%";

    partial void OnIsLoadingChanged(bool value) =>
        OnPropertyChanged(nameof(IsCurrentInstanceProcessing));

    partial void OnIsGameRunningChanged(bool value) =>
        OnPropertyChanged(nameof(IsCurrentInstanceProcessing));
}