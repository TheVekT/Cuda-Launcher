using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using Launcher.Core.Services;
using Launcher.Core.Services.Integrations;
using Launcher.UI.WPF.Helpers;
using Launcher.UI.WPF.Services;
using Launcher.UI.WPF.Stores;

namespace Launcher.UI.WPF.ViewModels.Game;

public class PlayViewModel: INotifyPropertyChanged
{
    //Services
    private readonly IDiscordService _discordService;
    
    //Stores
    private readonly SettingsStore _settingsStore;
    private readonly InstancesStore _instancesStore;
    private readonly AppStore _appStore;
    
    private double _downloadProgress;
    private string _downloadStatusText = "Initiating...";
    private string _downloadPercentText = "0%";
    
    private object _currentPlayButtonIcon;
    public DynamicTranslation CurrentPlayButtonText { get; } = new DynamicTranslation("Play.PlayButton");
    
    public AppStore AppStore => _appStore;
    
    //Commands
    public ICommand LaunchCommand => new RelayCommand(o => RequestLaunch?.Invoke());
    
    //Events
    public event Action RequestLaunch;

    public PlayViewModel(IDiscordService discordService, SettingsStore settingsStore, InstancesStore instancesStore, AppStore appStore)
    {
        _discordService = discordService;
        
        _settingsStore = settingsStore;
        _instancesStore = instancesStore;
        _appStore = appStore;
        
        _settingsStore.PropertyChanged += OnSettingsStorePropertyChanged;
        
        
        ChangeToPlayIcon("Icon.Play");
    }
    
    private void OnSettingsStorePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SettingsStore.IsEnableDiscordRichPresence))
        {
            UpdateDiscordPresence();
        }
    }

    public void UpdateDiscordPresence()
    {
        if (_settingsStore.IsEnableDiscordRichPresence)
        {
            if(_appStore.IsGameRunning) _discordService.SetPlayingPresence(_instancesStore.SelectedInstance);
            else _discordService.SetMenuPresence();
        }
        else
        {
            _discordService.ClearPresence();
        }
    }
    
    public void ChangeToPlayIcon(string path)
    {
        CurrentPlayButtonIcon = Application.Current.TryFindResource(path);
    }
    
    //Getters and Setters
    public object CurrentPlayButtonIcon
    {
        get => _currentPlayButtonIcon;
        set
        {
            if (_currentPlayButtonIcon != value)
            {
                _currentPlayButtonIcon = value;
                OnPropertyChanged(nameof(CurrentPlayButtonIcon));
            }
        }
    }
    

    
    public double DownloadProgress
    {
        get => _downloadProgress;
        set
        {
            if (Math.Abs(_downloadProgress - value) > 0.01)
            {
                _downloadProgress = value;
                OnPropertyChanged(nameof(DownloadProgress));
                DownloadPercentText = $"{value:0}%";
            }
        }
    }
    
    // 3. Текст статуса (например "DOWNLOADING ASSETS")
    public string DownloadStatusText
    {
        get => _downloadStatusText;
        set
        {
            if (_downloadStatusText != value)
            {
                _downloadStatusText = value;
                OnPropertyChanged(nameof(DownloadStatusText));
            }
        }
    }

    // 4. Текст процентов (отдельно для правого TextBlock)
    public string DownloadPercentText
    {
        get => _downloadPercentText;
        set
        {
            if (_downloadPercentText != value)
            {
                _downloadPercentText = value;
                OnPropertyChanged(nameof(DownloadPercentText));
            }
        }
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    
}
