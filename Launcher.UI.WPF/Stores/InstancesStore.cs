using CommunityToolkit.Mvvm.ComponentModel;
using Launcher.Core.Instances.Abstractions;
using Launcher.Core.Instances.Models;
using Launcher.Core.System.Abstractions;
using Launcher.Infrastructure.Assets.Abstractions;
using Launcher.Infrastructure.Config.Abstractions;
using Launcher.Infrastructure.Config.Models;
using Launcher.UI.WPF.Helpers;
using Launcher.UI.WPF.Services.Abstractions;

namespace Launcher.UI.WPF.Stores;

public partial class InstancesStore: ObservableObject
{
    //Services
    private readonly IInstanceFileSystemService _instanceFileSystemService;
    private readonly IInstanceService _instanceService;
    private readonly ISettingsService _settingsService;
    private readonly IIconsService _iconsService;
    private readonly IFileDialogService _fileDialogService;

    //Attributes
    [ObservableProperty]
    private MinecraftInstance? _selectedInstance;
    [ObservableProperty]
    [property: SettingProperty]
    private string? _lastSelectedInstanceId;
    [ObservableProperty]
    [property: SettingProperty]
    private int _selectedSortIndex;
    
    //Collections
    public ObservableRangeCollection<MinecraftInstance> Instances { get; } = new();
    
    public ObservableRangeCollection<string> IconList { get; } = new();
    
    public InstancesStore(
        IInstanceFileSystemService instanceFileSystemService,
        IInstanceService instanceService,
        ISettingsService settingsService,
        IIconsService iconsService,
        IFileDialogService fileDialogService)
    {
        _instanceFileSystemService = instanceFileSystemService;
        _instanceService = instanceService;
        _settingsService = settingsService;
        _iconsService = iconsService;
        _fileDialogService = fileDialogService;

        LoadIcons();
        LoadSavedInstances();
        
        _settingsService.Initialize(this);

        if (!string.IsNullOrEmpty(LastSelectedInstanceId))
        {
            var lastSelected = Instances.FirstOrDefault(i => i.Id == LastSelectedInstanceId);
            if (lastSelected != null)
            {
                SelectedInstance = lastSelected;
            }
            else
            {
                SelectedInstance = Instances.FirstOrDefault();
            }
        }
        ApplySort();
    }
    
    public void SelectIconFromFileDialog()
    {
        var filePaths = _fileDialogService.OpenMultipleFiles(
            filter: "Image Files|*.png;*.jpg;*.jpeg;*.ico;*.gif", 
            title: "Select icon");

        if (filePaths != null && filePaths.Length > 0)
        {
            ProcessIconFile(filePaths);
        }
    }

    public void HandleIconDrop(string[]? files)
    {
        if (files != null && files.Length > 0)
        {
            ProcessIconFile(files);
        }
    }

    private void ProcessIconFile(string[]? files)
    {
        foreach (var filePath in files ?? Array.Empty<string>())
            _iconsService.ImportIcon(filePath);
    
        if (files?.Length > 0)
        {
            LoadIcons();
        }
        else
        {
            Console.WriteLine("[Error] Не удалось импортировать иконку.");
        }
    }
    
    
    private void LoadSavedInstances()
    {
        var loaded = _instanceService.LoadInstances();
        Instances.ReplaceRange(loaded);
        
        if (Instances.Count > 0) SelectedInstance = Instances[0];
    }
    
    public void DeleteInstance(MinecraftInstance instance)
    {
        bool wasSelected = (SelectedInstance == instance);
        
        _instanceService.DeleteInstance(instance.Id);
        
        Instances.Remove(instance);
        
        _instanceFileSystemService.DeleteInstance(instance);
        
        if (wasSelected)
        {
            SelectedInstance = Instances.FirstOrDefault() ;
        }
    }
    
    private void LoadIcons()
    {
        var icons = _iconsService.GetAvailableIcons();
    
        IconList.ReplaceRange(icons);
        OnPropertyChanged(nameof(IconList));
    }
    
    public void ApplySort()
    {
        var lastSelectedInstance = SelectedInstance;
        // Берем текущие элементы
        var items = Instances.ToList();
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
        Instances.ReplaceRange(sortedItems);

        SelectedInstance = lastSelectedInstance;
    }
    
    //Getters and Setters
    
    partial void OnSelectedInstanceChanged(MinecraftInstance? value)
    {
        if (value != null) LastSelectedInstanceId = value.Id;
        else LastSelectedInstanceId = null;
    }
    
    partial void OnSelectedSortIndexChanged(int value)
    {
        ApplySort();
    }
}