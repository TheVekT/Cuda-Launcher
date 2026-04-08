using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Launcher.Core.Services.System;
using Launcher.UI.WPF.Messages;
using Launcher.UI.WPF.Services;
using Launcher.UI.WPF.ViewModels.Game;

namespace Launcher.UI.WPF.Stores;

public partial class AppStore: ObservableObject, IRecipient<ThemeChangedMessage>
{
    private readonly ThemeService _themeService;
    private readonly ILauncherPathsService _pathsService;
    
    [ObservableProperty]
    private object? _currentOverlayView;
    [ObservableProperty]
    private bool _isOverlayVisible;
    private string _themeBannerPath;
    [ObservableProperty]
    private bool _isDownloading;
    [ObservableProperty]
    private bool _isGameRunning;
    [ObservableProperty]
    private bool _isDragDropActive;
    [ObservableProperty]
    private object _currentView;
    [ObservableProperty]
    private bool _showCompactPlayButton;

    public AppStore(ThemeService themeService, ILauncherPathsService pathsService)
    {
        _themeService = themeService;
        _pathsService = pathsService;
        
        ThemeBannerPath = themeService.CurrentTheme.BannerPath;
        
        WeakReferenceMessenger.Default.RegisterAll(this);
    }

    public void Receive(ThemeChangedMessage message)
    {
        var theme = _themeService.GetThemeByFileName(message.ThemeFileName);
        ThemeBannerPath = theme.BannerPath;
    }

    //Getters and Setters
    public bool IsCurrentInstanceProcessing
    {
        get
        {
            if (IsGameRunning || IsDownloading) return true;
            return false;
        }
    }

    public string ThemeBannerPath
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
            OnPropertyChanged(nameof(ThemeBannerPath));
        }
    }

    public string CurrentBannerPath
    {
        get => ThemeBannerPath;
    }
    partial void OnCurrentOverlayViewChanged(object? value)
    {
        IsOverlayVisible = value != null;
    }
    
    partial void OnCurrentViewChanged(object value)
    {
        ShowCompactPlayButton = !(value is PlayViewModel);
    }
}