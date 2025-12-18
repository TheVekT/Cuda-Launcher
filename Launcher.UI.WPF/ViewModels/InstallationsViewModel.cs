using System.ComponentModel;
using System.Windows.Input;
using Launcher.UI.WPF.Helpers;
using Launcher.UI.WPF.Services;
using System.Collections.ObjectModel;
using System.IO;
using System.Diagnostics;

namespace Launcher.UI.WPF.ViewModels;

public class InstallationsViewModel : INotifyPropertyChanged
{
    public ICommand OpenAddVersionCommand { get; }
    public ICommand ToggleCreatingPageCommand { get; }
    
    public ICommand CloseOverlayCommand { get; }
    
    public ObservableCollection<string> IconList { get; } = new();
    
    
    public bool CreatingPage1Visible { get; set; } = false;
    public bool CreatingPage2Visible { get; set; } = true;
    
    public InstallationsViewModel(MainViewModel mainViewModel){
        OpenAddVersionCommand = new RelayCommand(o => 
        {
            LoadIcons();

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
    
    

   
    
    private readonly List<string> _ignoredIcons = new()
    {
        "example.png"
    };
    
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
                if (!_ignoredIcons.Contains(fileName))
                {
                    IconList.Add(file);
                }
            }
        }
    }

    
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}