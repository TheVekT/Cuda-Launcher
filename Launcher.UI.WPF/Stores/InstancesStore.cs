using CommunityToolkit.Mvvm.ComponentModel;
using Launcher.Core.Instances.Abstractions;
using Launcher.Core.Instances.Models;
using Launcher.Infrastructure.Assets.Abstractions;
using Launcher.Infrastructure.Config.Abstractions;
using Launcher.Infrastructure.Config.Models;
using Launcher.UI.WPF.Helpers.Collections;
using Launcher.UI.WPF.Services.Windows.Abstractions;

namespace Launcher.UI.WPF.Stores;

public partial class InstancesStore: ObservableObject
{
    //Services
    private readonly IInstanceFileSystemService _instanceFileSystemService;
    private readonly IInstanceService _instanceService;
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
        _iconsService = iconsService;
        _fileDialogService = fileDialogService;

        LoadIcons();
        LoadSavedInstances();
        
        settingsService.Initialize(this);

        if (!string.IsNullOrEmpty(LastSelectedInstanceId))
        {
            var lastSelected = Instances.FirstOrDefault(i => i.Id == LastSelectedInstanceId);
            SelectedInstance = lastSelected ?? Instances.FirstOrDefault();
        }
        ApplySort(SelectedSortIndex);
    }
    
    public void SelectIconFromFileDialog()
    {
        var filePaths = _fileDialogService.OpenMultipleFiles(
            filter: "Image Files|*.png;*.jpg;*.jpeg;*.ico;*.gif", 
            title: "Select icon");

        if (filePaths is { Length: > 0 })
        {
            ProcessIconFile(filePaths);
        }
    }

    public void HandleIconDrop(string[]? files)
    {
        if (files is { Length: > 0 })
        {
            ProcessIconFile(files);
        }
    }

    private void ProcessIconFile(string[]? files)
    {
        foreach (var filePath in files ?? [])
            _iconsService.ImportIcon(filePath);
    
        if (files?.Length > 0)
        {
            LoadIcons();
        }
        else
        {
            Console.WriteLine("[Error] Cannot import icon.");
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
    
    public void ApplySort(int sortIndex)
    {
        var lastSelectedInstance = SelectedInstance;
        // Берем текущие элементы
        var items = Instances.ToList();
        IEnumerable<MinecraftInstance> sortedItems;

        switch (sortIndex)
        {
            case 0: // Last Played
                sortedItems = items.OrderByDescending(x => x.LastPlayedDate.HasValue)
                    .ThenByDescending(x => x.LastPlayedDate);
                break;

            case 1: // Name
                sortedItems = items.OrderBy(x => x.Name);
                break;

            case 2: // Game Version 
                // Using TryParse to sort versions correctly, treating them as Version objects rather than strings
                sortedItems = items.OrderByDescending(x => 
                {
                    if (Version.TryParse(x.GameVersion, out var v)) return v;
                    return new Version(0, 0);
                });
                break;

            case 3: // Mod Loader
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
    
    partial void OnSelectedSortIndexChanged(int value) =>
        ApplySort(value);
}
