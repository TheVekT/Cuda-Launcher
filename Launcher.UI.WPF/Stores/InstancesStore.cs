using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Launcher.Core.Models;
using Launcher.Core.Services.IO;
using Launcher.UI.WPF.Helpers;
using Microsoft.Win32;

namespace Launcher.UI.WPF.Stores;

public class InstancesStore: INotifyPropertyChanged
{
    //Services
    private readonly IInstanceFileSystemService _instanceFileSystemService;
    private readonly IInstanceService _instanceService;
    private readonly SettingsService _settingsService;
    
    //Attributes
    private MinecraftInstance? _selectedInstance;
    private readonly List<string> _ignoredIcons = new() { "example.png" };
    private string? _lastSelectedInstanceId;
    private int _selectedSortIndex;
    
    //Collections
    public ObservableCollection<MinecraftInstance> Instances { get; set; } = new();
    
    public ObservableCollection<string> IconList { get; } = new();
    
    //Events
    public event Action AddedInstance;
    
    //Commands
    public ICommand SelectIconCommand { get; }
    public ICommand DropIconCommand { get; }

    
    public InstancesStore(IInstanceFileSystemService instanceFileSystemService, IInstanceService instanceService, SettingsService settingsService)
    {
        _instanceFileSystemService = instanceFileSystemService;
        _instanceService = instanceService;
        _settingsService = settingsService;
        
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
        AddedInstance += () => ApplySort();
        ApplySort();
        
        SelectIconCommand = new RelayCommand(_ => SelectIconFromFileDialog());
        DropIconCommand = new RelayCommand(HandleIconDrop);
    }
    
    private void SelectIconFromFileDialog()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Image Files|*.png;*.jpg;*.jpeg;*.ico;*.gif",
            Title = "Выберите иконку"
        };

        if (dialog.ShowDialog() == true)
        {
            ProcessIconFile(dialog.FileName);
        }
    }

    private void HandleIconDrop(object parameter)
    {
        if (parameter is DragEventArgs e && e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length > 0)
            {
                // Берем только первый файл, так как иконка может быть только одна
                string file = files[0];
                string ext = Path.GetExtension(file).ToLowerInvariant();

                if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".ico" || ext == ".gif")
                {
                    ProcessIconFile(file);
                }
                else
                {
                    // Тут можно вызвать твой NotificationService.Instance.ShowWarning(...)
                    // чтобы сообщить, что формат файла не поддерживается
                }
            }
            e.Handled = true;
        }
    }

    private void ProcessIconFile(string filePath)
    {
        //Copy the selected file to the icons directory
        var iconsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Icons");
        var destPath = Path.Combine(iconsDir, Path.GetFileName(filePath));
        try        {
            File.Copy(filePath, destPath, overwrite: true);
        }
        catch (Exception ex)        {
            // Тут можно вызвать твой NotificationService.Instance.ShowError(...)
            Console.WriteLine($"[Error] Не удалось скопировать файл иконки: {ex.Message}");
            return;
        }
        
        LoadIcons();
        OnPropertyChanged(nameof(IconList));
    }
    
    public void InvokeAddedInstance() => AddedInstance?.Invoke();
    
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
        var iconsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Icons");

        if (Directory.Exists(iconsPath))
        {
            // Указываем все форматы, которые хотим поддерживать
            var supportedExtensions = new[] { ".png", ".jpg", ".jpeg", ".ico", ".gif" };

            // Перебираем все файлы в папке и оставляем только те, чье расширение есть в нашем массиве
            var files = Directory.EnumerateFiles(iconsPath)
                .Where(f => supportedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()));
        
            IconList.Clear();
            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                if (!_ignoredIcons.Contains(fileName)) 
                {
                    IconList.Add(file); 
                }
            }
        }
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
    [SettingProperty]
    public string? LastSelectedInstanceId
    {
        get => _lastSelectedInstanceId;
        set
        {
            if (_lastSelectedInstanceId != value)
            {
                _lastSelectedInstanceId = value;
                OnPropertyChanged(nameof(LastSelectedInstanceId));
            }
        }
    }
    
    public MinecraftInstance? SelectedInstance
    {
        get => _selectedInstance;
        set
        {
            if (_selectedInstance != value)
            {
                _selectedInstance = value;
                if (value != null) LastSelectedInstanceId = value.Id;
                else LastSelectedInstanceId = null;
                OnPropertyChanged(nameof(SelectedInstance));
            }
        }
    }
    
    [SettingProperty]
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