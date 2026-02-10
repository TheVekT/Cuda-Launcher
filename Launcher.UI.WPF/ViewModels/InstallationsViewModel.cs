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
using Launcher.UI.WPF.Services;
using Launcher.UI.WPF.Stores;

namespace Launcher.UI.WPF.ViewModels;

public class InstallationsViewModel : INotifyPropertyChanged
{
    //Services
    private readonly IGameVersionService _versionService; 
    private readonly IInstanceService _instanceService;
    
    //Stores
    private readonly InstancesStore _instancesStore;
    private readonly AppStore _appStore;
    
    //Commands
    public ICommand DeleteInstanceCommand { get; }
    public ICommand OpenAddVersionCommand { get; }
    
    //public properties
    public InstancesStore InstancesStore => _instancesStore;
  
    
    public InstallationsViewModel(
        IGameVersionService versionService, 
        InstanceService instanceService,
        InstancesStore instancesStore,
        AppStore appStore)
    {
        _versionService = versionService;
        _instanceService = instanceService;
        
        _instancesStore = instancesStore;
        _appStore = appStore;
        
        
        DeleteInstanceCommand = new RelayCommand(o => 
        {
            if (o is MinecraftInstance instanceToDelete)
            {
                _instancesStore.DeleteInstance(instanceToDelete);
            }
        });

        OpenAddVersionCommand = new RelayCommand(async o => 
        {
            var menu = new Resources.Overlay.AddVersionMenu();
            var InstanceVM = new InstanceVM(_versionService, _instanceService, _instancesStore);
            await InstanceVM.InitializeAsync();
            menu.DataContext = InstanceVM; 
            InstanceVM.RequestClose += () => 
            {
                _appStore.CurrentOverlayView = null;
            };
            _appStore.CurrentOverlayView = menu;
        });
    }





    
    

    

    
    

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}