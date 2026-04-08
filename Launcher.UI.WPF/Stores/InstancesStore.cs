using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Launcher.Core.Models;
using Launcher.Core.Services.IO;
using Launcher.Core.Services.System;

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
    private readonly List<string> _ignoredIcons = new() { "example.png" };
    [ObservableProperty]
    [property: SettingProperty]
    private string? _lastSelectedInstanceId;
    [ObservableProperty]
    [property: SettingProperty]
    private int _selectedSortIndex;
    
    //Collections
    public ObservableCollection<MinecraftInstance> Instances { get; set; } = new();
    
    public ObservableCollection<string> IconList { get; } = new();
    
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
        var filePath = _fileDialogService.OpenFile(
            filter: "Image Files|*.png;*.jpg;*.jpeg;*.ico;*.gif", 
            title: "Select icon");

        if (!string.IsNullOrEmpty(filePath))
        {
            ProcessIconFile(filePath);
        }
    }

    public void HandleIconDrop(string[]? files)
    {
        if (files != null && files.Length > 0)
        {
            ProcessIconFile(files[0]);
        }
    }

    private void ProcessIconFile(string filePath)
    {
        var destPath = _iconsService.ImportIcon(filePath);
    
        if (destPath != null)
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
        Instances.Clear();
        foreach (var inst in loaded)
        {
            Instances.Add(inst);
        }
        
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
    
        IconList.Clear();
        foreach (var icon in icons)
        {
            IconList.Add(icon);
        }
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
        Instances.Clear();
        foreach (var item in sortedItems)
        {
            Instances.Add(item);
        }

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