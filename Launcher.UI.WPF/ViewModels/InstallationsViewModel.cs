using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Launcher.Core.Models;
using Launcher.Core.Services.Game;
using Launcher.Core.Services.IO;
using Launcher.UI.WPF.Helpers;
using Launcher.UI.WPF.Resources.Overlay;
using Launcher.UI.WPF.Services;
using Launcher.UI.WPF.Stores;

namespace Launcher.UI.WPF.ViewModels;

public class InstallationsViewModel : INotifyPropertyChanged
{
    //Services
    private readonly IGameVersionService _versionService; 
    private readonly IInstanceService _instanceService;
    private readonly IInstanceFileSystemService _instanceFileSystemService;
    
    //Stores
    private readonly InstancesStore _instancesStore;
    private readonly SettingsStore _settingsStore;
    private readonly AppStore _appStore;
    
    //Commands
    public ICommand DeleteInstanceCommand { get; }
    public ICommand OpenAddVersionCommand { get; }
    
    //Attributes
    
    
    //public properties
    public InstancesStore InstancesStore => _instancesStore;
  
    
    public InstallationsViewModel(
        IGameVersionService versionService, 
        IInstanceService instanceService,
        IInstanceFileSystemService instanceFileSystemService,
        InstancesStore instancesStore,
        SettingsStore settingsStore,
        AppStore appStore)
    {
        _versionService = versionService;
        _instanceService = instanceService;
        _instanceFileSystemService = instanceFileSystemService;
        
        _instancesStore = instancesStore;
        _settingsStore = settingsStore;
        _appStore = appStore;
        
        
        DeleteInstanceCommand = new RelayCommand(async o => await DeleteInstance(o as MinecraftInstance));

        OpenAddVersionCommand = new RelayCommand(async o => 
        {
            var menu = new AddVersionMenu();
            var InstanceVM = new InstanceVM(_versionService, _instanceService, _instanceFileSystemService, _instancesStore, _settingsStore);
            await InstanceVM.InitializeAsync();
            menu.DataContext = InstanceVM; 
            InstanceVM.RequestClose += () => 
            {
                _appStore.CurrentOverlayView = null;
            };
            _appStore.CurrentOverlayView = menu;
        });
    }
    
    private async Task DeleteInstance(MinecraftInstance instance)
    {
        var confirmVm = new ConfirmVM(
            string.Format(LocalizationService.Instance["Confirmation.DeleteInstanceTitle"], instance.Name), 
            string.Format(LocalizationService.Instance["Confirmation.DeleteInstanceMessage"], instance.Name),
            ConfirmButtons.Delete);
        var view = new ConfirmMenu();
        view.DataContext = confirmVm;
        _appStore.CurrentOverlayView = view;
        bool isConfirmed = await confirmVm.WaitAsync();
        _appStore.CurrentOverlayView = null;
        
        if (!isConfirmed)
        {
            return;
        }
        _instancesStore.DeleteInstance(instance);
    }

    
    
    //Getters and setters


    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}