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

namespace Launcher.UI.WPF.ViewModels;

public class InstallationsViewModel : INotifyPropertyChanged
{
    private readonly MainViewModel _mainViewModel;
    private readonly IGameVersionService _versionService; 
    private readonly IInstanceService _instanceService;
    
    public ICommand DeleteInstanceCommand { get; }
    public ICommand OpenAddVersionCommand { get; }
    public ICommand ToggleCreatingPageCommand { get; }
    public ICommand CloseOverlayCommand { get; }
    public ICommand CreateInstanceCommand { get; }
    
    public ObservableCollection<string> IconList { get; } = new();
    public ObservableCollection<MinecraftInstance> Instances => _mainViewModel.Instances;
    public ObservableCollection<string> GameVersions { get; } = new(); 
    public ObservableCollection<IsolationType> IsolationTypes { get; } = new()
    {
        IsolationType.Global,
        IsolationType.Full,
        IsolationType.Partial
    };
    
    public bool CreatingPage1Visible { get; set; } = false;
    public bool CreatingPage2Visible { get; set; } = true;

    // --- НОВОЕ СВОЙСТВО: ВЫБРАННАЯ ИКОНКА ---
    private string _selectedIcon;
    public string SelectedIcon
    {
        get => _selectedIcon;
        set
        {
            if (_selectedIcon != value)
            {
                _selectedIcon = value;
                OnPropertyChanged(nameof(SelectedIcon));
            }
        }
    }

    // --- Свойства ввода ---
    private string _selectedGameVersion;
    public string SelectedGameVersion
    {
        get => _selectedGameVersion;
        set
        {
            if (_selectedGameVersion != value)
            {
                _selectedGameVersion = value;
                OnPropertyChanged(nameof(SelectedGameVersion));
                OnPropertyChanged(nameof(SuggestedName));
            }
        }
    }
    
    private string _installationName;
    public string InstallationName
    {
        get => _installationName;
        set
        {
            if (_installationName != value)
            {
                _installationName = value;
                OnPropertyChanged(nameof(InstallationName));
            }
        }
    }

    private IsolationType _selectedIsolation = IsolationType.Global;
    public IsolationType SelectedIsolation
    {
        get => _selectedIsolation;
        set
        {
            if (_selectedIsolation != value)
            {
                _selectedIsolation = value;
                OnPropertyChanged(nameof(SelectedIsolation));
            }
        }
    }
    
    public string SuggestedName
    {
        get
        {
            if (string.IsNullOrEmpty(SelectedGameVersion)) return "New Installation";
            if (SelectedModLoader == "Vanilla") return $"Version {SelectedGameVersion}";
            return $"{SelectedModLoader} {SelectedGameVersion}";
        }
    }
    
    private string _selectedModLoader;
    public string SelectedModLoader
    {
        get => _selectedModLoader;
        set
        {
            if (_selectedModLoader != value)
            {
                _selectedModLoader = value;
                OnPropertyChanged(nameof(SelectedModLoader));
                _ = RefreshGameVersions();
                OnPropertyChanged(nameof(SuggestedName));
            }
        }
    }
    
    public async Task InitializeAsync()
    {
        await RefreshGameVersions();
    }

    public InstallationsViewModel(
        MainViewModel mainViewModel, 
        IGameVersionService versionService, 
        InstanceService instanceService)
    {
        _mainViewModel = mainViewModel;
        _versionService = versionService;
        _instanceService = instanceService;
        
        _selectedModLoader = "Vanilla"; 
        
        CreateInstanceCommand = new RelayCommand(o => CreateInstance());
        
        DeleteInstanceCommand = new RelayCommand(o => 
        {
            
            if (o is MinecraftInstance instanceToDelete)
            {
                DeleteInstance(instanceToDelete);
            }
        });

        OpenAddVersionCommand = new RelayCommand(async o => 
        {
            LoadIcons();
            
            InstallationName = string.Empty;
            SelectedIsolation = IsolationType.Global;

            bool typeChanged = _selectedModLoader != "Vanilla";
            _selectedModLoader = "Vanilla"; 
            OnPropertyChanged(nameof(SelectedModLoader)); 

            await RefreshGameVersions();

            var menu = new Resources.Overlay.AddVersionMenu();
            CreatingPage1Visible = true;
            CreatingPage2Visible = false;
            menu.DataContext = this; 
            
            mainViewModel.CurrentOverlayView = menu;
        });

        ToggleCreatingPageCommand = new RelayCommand(o =>
        {
            CreatingPage1Visible = !CreatingPage1Visible;
            CreatingPage2Visible = !CreatingPage2Visible;
            OnPropertyChanged(nameof(CreatingPage1Visible));
            OnPropertyChanged(nameof(CreatingPage2Visible));
        });

        CloseOverlayCommand = new RelayCommand(o =>
        {
            mainViewModel.CloseOverlayCommand.Execute(o);
        });
    }

    private void CreateInstance()
    {
        if (string.IsNullOrEmpty(SelectedGameVersion)) return;

        var finalName = string.IsNullOrWhiteSpace(InstallationName) 
            ? SuggestedName 
            : InstallationName;

        var newInstance = new MinecraftInstance
        {
            Name = finalName,
            GameVersion = SelectedGameVersion,
            LoaderType = GetLoaderType(SelectedModLoader),
            IsolationType = SelectedIsolation,
            
            IconPath = SelectedIcon ?? IconList.FirstOrDefault(), 
            
            LoaderVersion = (SelectedModLoader == "Vanilla") ? null : "Auto"
        };

        _mainViewModel.Instances.Add(newInstance);
        _instanceService.SaveInstances(_mainViewModel.Instances);
        CloseOverlayCommand.Execute(null);
        Debug.WriteLine($"Created instance: {finalName}");
    }

    private async Task RefreshGameVersions()
    {
        try 
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() => SelectedGameVersion = null);

            GameLoaderType type = GetLoaderType(_selectedModLoader);
            
            var loadedVersions = await _versionService.GetGameVersionsByTypeAsync(type);
            var versionList = loadedVersions.ToList(); 

            System.Windows.Application.Current.Dispatcher.Invoke(() => 
            {
                GameVersions.Clear();
                if (versionList.Count == 0) return;
                foreach (var version in versionList) GameVersions.Add(version);
            });
            
            await Task.Delay(50);

            System.Windows.Application.Current.Dispatcher.Invoke(() => 
            {
                if (GameVersions.Count > 0) SelectedGameVersion = GameVersions[0];
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[VM] Ошибка RefreshGameVersions: {ex.Message}");
        }
    }

    private GameLoaderType GetLoaderType(string uiName)
    {
        return uiName switch
        {
            "Forge" => GameLoaderType.Forge,
            "NeoForge" => GameLoaderType.NeoForge,
            "Fabric" => GameLoaderType.Fabric,
            "Quilt" => GameLoaderType.Quilt,
            _ => GameLoaderType.Vanilla
        };
    }
    
    private readonly List<string> _ignoredIcons = new() { "example.png" };
    
    private void LoadIcons()
    {
        var iconsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Icons");
        if (Directory.Exists(iconsPath))
        {
            var files = Directory.GetFiles(iconsPath, "*.png"); 
            IconList.Clear();
            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                if (!_ignoredIcons.Contains(fileName)) IconList.Add(file);
            }

            // --- ВАЖНО: Выбираем первую иконку по умолчанию ---
            if (IconList.Count > 0)
            {
                SelectedIcon = IconList[0];
            }
        }
    }
    
    private void DeleteInstance(MinecraftInstance instance)
    {
        _instanceService.DeleteInstance(instance.Id);
        
        _mainViewModel.Instances.Remove(instance);

        if (_mainViewModel.SelectedInstance == instance)
        {
            _mainViewModel.Instances.FirstOrDefault();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}