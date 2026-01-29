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
using Launcher.UI.WPF.Helpers;
using Launcher.UI.WPF.Services;

namespace Launcher.UI.WPF.ViewModels;

public class InstallationsViewModel : INotifyPropertyChanged
{
    private readonly IGameVersionService _versionService; 

    public ICommand OpenAddVersionCommand { get; }
    public ICommand ToggleCreatingPageCommand { get; }
    public ICommand CloseOverlayCommand { get; }
    
    public ObservableCollection<string> IconList { get; } = new();
    
    public ObservableCollection<string> GameVersions { get; } = new(); 
    
    public bool CreatingPage1Visible { get; set; } = false;
    public bool CreatingPage2Visible { get; set; } = true;

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
                
                // Уведомляем UI, что подсказка тоже изменилась!
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
    
    public string SuggestedName
    {
        get
        {
            // Логика "Маленькой утилиты" прямо здесь
            if (string.IsNullOrEmpty(SelectedGameVersion))
                return "New Installation";
                
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

                // Уведомляем UI, что подсказка тоже изменилась!
                OnPropertyChanged(nameof(SuggestedName)); 
            }
        }
    }
    
    public async Task InitializeAsync()
    {
        await RefreshGameVersions();
    }
    public InstallationsViewModel(MainViewModel mainViewModel, IGameVersionService versionService)
    {
        _versionService = versionService;
        _selectedModLoader = "Vanilla"; 
        
        
        OpenAddVersionCommand = new RelayCommand(async o => 
        {
            LoadIcons();
            
            // --- ИСПРАВЛЕНИЕ 1: Избегаем двойного обновления ---
            // Меняем поле напрямую, чтобы НЕ вызывать RefreshGameVersions через сеттер
            bool typeChanged = _selectedModLoader != "Vanilla";
            _selectedModLoader = "Vanilla"; 
            OnPropertyChanged(nameof(SelectedModLoader)); // Обновляем UI (ComboBox лоадеров)

            // Теперь вызываем обновление версий вручную один раз
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

    private async Task RefreshGameVersions()
    {
        try 
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() => SelectedGameVersion = null);

            GameLoaderType type = GetLoaderType(_selectedModLoader); // Используем поле, оно актуально
            

            // 2. Фоновая загрузка данных (не блокирует UI)
            var loadedVersions = await _versionService.GetGameVersionsByTypeAsync(type);
            var versionList = loadedVersions.ToList(); 

            // 3. Обновление UI (Коллекция + Выбор)
            // Делаем это внутри Invoke, чтобы события шли последовательно в UI потоке
            System.Windows.Application.Current.Dispatcher.Invoke(() => 
            {
                GameVersions.Clear();

                if (versionList.Count == 0) return;

                foreach (var version in versionList)
                {
                    GameVersions.Add(version);
                }
            });
            
            System.Windows.Application.Current.Dispatcher.Invoke(() => 
            {
                if (GameVersions.Count > 0)
                {
                    SelectedGameVersion = GameVersions[0];
                }
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
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}