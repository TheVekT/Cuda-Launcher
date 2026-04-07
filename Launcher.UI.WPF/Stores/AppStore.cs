using System.ComponentModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Launcher.UI.WPF.Messages;
using Launcher.UI.WPF.Services;
using Launcher.UI.WPF.ViewModels.Game;

namespace Launcher.UI.WPF.Stores;

public partial class AppStore: ObservableObject, IRecipient<ThemeChangedMessage>
{
    private readonly ThemeService _themeService;
    
    [ObservableProperty]
    private object? _currentOverlayView;
    [ObservableProperty]
    private bool _isOverlayVisible;
    private string _themeBannerPath = "Assets/Images/banner-default.jpg";
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DownloadPanelVisibility))]
    private bool _isDownloading;
    [ObservableProperty]
    private bool _isGameRunning;
    [ObservableProperty]
    private bool _isDragDropActive;
    [ObservableProperty]
    private object _currentView;
    [ObservableProperty]
    private bool _showCompactPlayButton;
    
    public Visibility DownloadPanelVisibility => _isDownloading ? Visibility.Visible : Visibility.Collapsed;

    public AppStore(ThemeService themeService)
    {
        _themeService = themeService;
        
        ThemeBannerPath = themeService.CurrentTheme.BannerPath;
        
        WeakReferenceMessenger.Default.RegisterAll(this);
    }

    public void Receive(ThemeChangedMessage message)
    {
        var theme = _themeService.GetThemeByFileName(message.ThemeFileName);
        Console.WriteLine($"Changing banner path: {theme.Name}");
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
            if (_themeBannerPath == value) return;
            if (value is null)
            {
                _themeBannerPath = "Assets/Images/banner-default.jpg";
            }
            else
            {
                _themeBannerPath = value; 
            }
            OnPropertyChanged(nameof(CurrentBannerPath));
            OnPropertyChanged(nameof(ThemeBannerPath));
        }
    }

    public string CurrentBannerPath
    {
        get
        {
            return ThemeBannerPath;
        }
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