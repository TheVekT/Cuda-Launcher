using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Launcher.Core.Integrations.Abstractions;
using Launcher.UI.WPF.Helpers;
using Launcher.UI.WPF.Messages;
using Launcher.UI.WPF.Stores;

namespace Launcher.UI.WPF.ViewModels.Game;

public partial class PlayViewModel: ObservableObject
{
    //Services
    private readonly IDiscordService _discordService;
    
    //Stores
    private readonly SettingsStore _settingsStore;
    private readonly InstancesStore _instancesStore;
    private readonly AppStore _appStore;
    
    public AppStore AppStore => _appStore;

    public PlayViewModel(IDiscordService discordService, SettingsStore settingsStore, InstancesStore instancesStore, AppStore appStore)
    {
        _discordService = discordService;
        
        _settingsStore = settingsStore;
        _instancesStore = instancesStore;
        _appStore = appStore;
        
        _settingsStore.PropertyChanged += OnSettingsStorePropertyChanged;
        _appStore.PropertyChanged += OnAppStorePropertyChanged;
    }
    
    private void OnSettingsStorePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SettingsStore.IsEnableDiscordRichPresence))
        {
            UpdateDiscordPresence();
        }
    }

    private void OnAppStorePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AppStore.IsGameRunning))
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
    
    //Commands
    [RelayCommand]
    private void Launch()
    {
        WeakReferenceMessenger.Default.Send(new LaunchGameRequestMessage());
    }
}
