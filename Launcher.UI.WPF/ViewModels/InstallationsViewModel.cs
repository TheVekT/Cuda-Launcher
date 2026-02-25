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
    private readonly AppStore _appStore;
    
    //Commands
    public ICommand DeleteInstanceCommand { get; }
    public ICommand OpenAddVersionCommand { get; }
    
    //Attributes
    private int _selectedSortIndex;
    
    //public properties
    public InstancesStore InstancesStore => _instancesStore;
  
    
    public InstallationsViewModel(
        IGameVersionService versionService, 
        InstanceService instanceService,
        IInstanceFileSystemService instanceFileSystemService,
        InstancesStore instancesStore,
        AppStore appStore)
    {
        _versionService = versionService;
        _instanceService = instanceService;
        _instanceFileSystemService = instanceFileSystemService;
        
        _instancesStore = instancesStore;
        _appStore = appStore;
        
        
        DeleteInstanceCommand = new RelayCommand(async o => await DeleteInstance(o as MinecraftInstance));

        OpenAddVersionCommand = new RelayCommand(async o => 
        {
            var menu = new AddVersionMenu();
            var InstanceVM = new InstanceVM(_versionService, _instanceService, _instanceFileSystemService, _instancesStore);
            await InstanceVM.InitializeAsync();
            menu.DataContext = InstanceVM; 
            InstanceVM.RequestClose += () => 
            {
                _appStore.CurrentOverlayView = null;
            };
            _appStore.CurrentOverlayView = menu;
        });
        _instancesStore.AddedInstance += () =>
        {
            ApplySort();
        };
        ApplySort();
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

    private void ApplySort()
    {
        var lastSelectedInstance = _instancesStore.SelectedInstance;
        // Берем текущие элементы
        var items = _instancesStore.Instances.ToList();
        IEnumerable<MinecraftInstance> sortedItems = null;

        switch (_selectedSortIndex)
        {
            case 0: // Last Played (Сначала новые, null в конце)
                sortedItems = items.OrderByDescending(x => x.LastPlayedDate.HasValue)
                    .ThenByDescending(x => x.LastPlayedDate);
                break;

            case 1: // Name (А-Я)
                sortedItems = items.OrderBy(x => x.Name);
                break;

            case 2: // Game Version (Сначала новые версии: 1.20 -> 1.8)
                // Используем Version.TryParse, чтобы 1.10 было больше 1.2
                sortedItems = items.OrderByDescending(x => 
                {
                    // Пытаемся распарсить версию, чтобы сортировать как числа, а не как текст
                    if (Version.TryParse(x.GameVersion, out var v)) return v;
                    return new Version(0, 0); // Если версия нестандартная, кидаем вниз
                });
                break;

            case 3: // Mod Loader (Группировка по типу)
                sortedItems = items.OrderBy(x => x.LoaderType.ToString())
                    .ThenBy(x => x.GameVersion);
                break;
                
            default:
                return;
        }
        _instancesStore.Instances.Clear();
        foreach (var item in sortedItems)
        {
            _instancesStore.Instances.Add(item);
        }

        _instancesStore.SelectedInstance = lastSelectedInstance;
    }
    

    
    //Getters and setters
    public int SelectedSortIndex
    {
        get => _selectedSortIndex;
        set
        {
            if (_selectedSortIndex != value)
            {
                _selectedSortIndex = value;
                OnPropertyChanged(nameof(SelectedSortIndex));
                // Как только меняется выбор в ComboBox, запускаем сортировку
                ApplySort();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}