using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Launcher.Core.Messages;
using Launcher.Core.Services;
using Launcher.Core.Services.Integrations;
using Launcher.UI.WPF.Helpers;
using Launcher.UI.WPF.Services;
using Launcher.UI.WPF.Stores;

namespace Launcher.UI.WPF.ViewModels.Game;

public partial class PlayViewModel: ObservableObject,
    IRecipient<GameLaunchProgressMessage>,
    IRecipient<GameLaunchStateMessage>
{
    //Services
    private readonly IDiscordService _discordService;
    
    //Stores
    private readonly SettingsStore _settingsStore;
    private readonly InstancesStore _instancesStore;
    private readonly AppStore _appStore;
    

    private double _downloadProgress;
    [ObservableProperty]
    private string _downloadStatusText = "Initiating...";
    [ObservableProperty]
    private string _downloadPercentText = "0%";
    [ObservableProperty]
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
        WeakReferenceMessenger.Default.RegisterAll(this);
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

    public void Receive(GameLaunchProgressMessage message)
    {
        _appStore.IsDownloading = true;
        OnPropertyChanged(nameof(_appStore.DownloadPanelVisibility));
        DownloadStatusText = message.Status;
        DownloadProgress = message.Percent;
    }

    public void Receive(GameLaunchStateMessage message)
    {
        if (message.IsRunning) {
            _appStore.IsGameRunning = true;
            UpdateDiscordPresence();
            _appStore.IsDownloading = false;
            OnPropertyChanged(nameof(_appStore.DownloadPanelVisibility)); 
            CurrentPlayButtonText.Update("Play.PlayButton.Close"); 
            ChangeToPlayIcon("Icon.Close");
        }
        else
        {
            _appStore.IsDownloading = false;
            _appStore.IsGameRunning = false;
            OnPropertyChanged(nameof(_appStore.DownloadPanelVisibility)); 
            CurrentPlayButtonText.Update("Play.PlayButton");
            UpdateDiscordPresence();
            ChangeToPlayIcon("Icon.Play");
        }
    }

    //Getters and Setters
    
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
}
