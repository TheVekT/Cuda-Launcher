using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using System.Windows.Threading;
using Launcher.Core.Models;
using Launcher.Core.Services.Game;
using Launcher.Core.Services.IO;
using Launcher.UI.WPF.Helpers;
using Launcher.UI.WPF.Stores;

namespace Launcher.UI.WPF.ViewModels;

public class InstanceVM: INotifyPropertyChanged
{
    //Services
    private readonly IGameVersionService _versionService;
    private readonly IInstanceService _instanceService;
    //Stores
    private readonly InstancesStore _instancesStore;
    
    public bool CreatingPage1Visible { get; set; } = false;
    public bool CreatingPage2Visible { get; set; } = true;
    
    private string _selectedIcon;
    private string _selectedGameVersion;
    private string _installationName;
    private string _selectedModLoader;
    private IsolationType _selectedIsolation = IsolationType.Global;
    private readonly List<string> _ignoredIcons = new() { "example.png" };
    
    //Commands
    public ICommand ToggleCreatingPageCommand { get; }
    public ICommand CloseSelfCommand { get; }
    public ICommand CreateInstanceCommand => new RelayCommand(o => CreateInstance());
    
    //Events
    public event Action RequestClose;
    
    //Collections
    public ObservableCollection<string> IconList { get; } = new();
    public ObservableCollection<string> GameVersions { get; } = new(); 
    
    //public properties
    public InstancesStore InstancesStore => _instancesStore;
    
    public InstanceVM(IGameVersionService versionService, IInstanceService instanceService,InstancesStore instancesStore)
    {
        _versionService = versionService;
        _instanceService = instanceService;
        _instancesStore = instancesStore;
        
        InstallationName = string.Empty;
        SelectedIsolation = IsolationType.Global;
        
        bool typeChanged = _selectedModLoader != "Vanilla";
        _selectedModLoader = "Vanilla"; 
        
        OnPropertyChanged(nameof(SelectedModLoader)); 
        
        CreatingPage1Visible = true;
        CreatingPage2Visible = false;
        
        LoadIcons();
        
        ToggleCreatingPageCommand = new RelayCommand(o =>
        {
            CreatingPage1Visible = !CreatingPage1Visible;
            CreatingPage2Visible = !CreatingPage2Visible;
            OnPropertyChanged(nameof(CreatingPage1Visible));
            OnPropertyChanged(nameof(CreatingPage2Visible));
        });
        CloseSelfCommand = new RelayCommand(o => RequestClose?.Invoke());
        
    }
    public async Task InitializeAsync()
    {
        await RefreshGameVersions();
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
            if (IconList.Count > 0)
            {
                SelectedIcon = IconList[0];
            }
        }
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
        _instancesStore.Instances.Add(newInstance);
        _instanceService.SaveInstances(_instancesStore.Instances);
        RequestClose?.Invoke();
        Debug.WriteLine($"Created instance: {finalName}");
        _instancesStore.InvokeAddedInstance();
        _instancesStore.SelectedInstance = newInstance;
    }
    
    
    //Getters and Setters
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
            return $"{SelectedModLoader} {SelectedGameVersion}";
        }
    }
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
    
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}