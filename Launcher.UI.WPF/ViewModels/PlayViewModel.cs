using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using Launcher.UI.WPF.Helpers;
using Launcher.UI.WPF.Services;

namespace Launcher.UI.WPF.ViewModels
{
    
    public class PlayViewModel: INotifyPropertyChanged
    {
        private double _downloadProgress;
        private string _downloadStatusText = "Initiating...";
        private string _downloadPercentText = "0%";
        private bool _isDownloading;
        private bool _isGameRunning;
        
        private object _currentPlayButtonIcon;
        public DynamicTranslation CurrentPlayButtonText { get; } = new DynamicTranslation("Play.PlayButton");
        
        public Visibility DownloadPanelVisibility => _isDownloading ? Visibility.Visible : Visibility.Collapsed;
        
        //Commands
        public ICommand LaunchCommand => new RelayCommand(o => RequestLaunch?.Invoke());
        
        //Events
        public event Action RequestLaunch;

        public PlayViewModel()
        {
            ChangeToPlayIcon("Icon.Play");
        }
        
        public void ChangeToPlayIcon(string path)
        {
            CurrentPlayButtonIcon = Application.Current.TryFindResource(path);
        }
        
        //Getters and Setters
        public object CurrentPlayButtonIcon
        {
            get => _currentPlayButtonIcon;
            set
            {
                if (_currentPlayButtonIcon != value)
                {
                    _currentPlayButtonIcon = value;
                    OnPropertyChanged(nameof(CurrentPlayButtonIcon));
                }
            }
        }
        
        public bool IsGameRunning
        {
            get => _isGameRunning;
            set
            {
                if (_isGameRunning != value)
                {
                    _isGameRunning = value;
                    OnPropertyChanged(nameof(IsGameRunning));
                }
            }
        }
        
        public double DownloadProgress
        {
            get => _downloadProgress;
            set
            {
                if (Math.Abs(_downloadProgress - value) > 0.01)
                {
                    _downloadProgress = value;
                    OnPropertyChanged(nameof(DownloadProgress));
                    DownloadPercentText = $"{value:0}%";
                }
            }
        }
        public bool IsDownloading
        {
            get => _isDownloading;
            set
            {
                if (_isDownloading != value)
                {
                    _isDownloading = value;
                    OnPropertyChanged(nameof(IsDownloading));
                    OnPropertyChanged(nameof(DownloadPanelVisibility)); 
                }
            }
        }
        // 3. Текст статуса (например "DOWNLOADING ASSETS")
        public string DownloadStatusText
        {
            get => _downloadStatusText;
            set
            {
                if (_downloadStatusText != value)
                {
                    _downloadStatusText = value;
                    OnPropertyChanged(nameof(DownloadStatusText));
                }
            }
        }

        // 4. Текст процентов (отдельно для правого TextBlock)
        public string DownloadPercentText
        {
            get => _downloadPercentText;
            set
            {
                if (_downloadPercentText != value)
                {
                    _downloadPercentText = value;
                    OnPropertyChanged(nameof(DownloadPercentText));
                }
            }
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        
    }
}