using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using Launcher.UI.WPF.Helpers;

namespace Launcher.UI.WPF.ViewModels
{
    
    public class PlayViewModel: INotifyPropertyChanged
    {
        private double _downloadProgress;
        private string _downloadStatusText = "Initiating...";
        private string _downloadPercentText = "0%";
        private bool _isDownloading;
        
        public Visibility DownloadPanelVisibility => _isDownloading ? Visibility.Visible : Visibility.Collapsed;
        public Boolean PlayButtonEnabled => !_isDownloading ;
        
        //Commands
        public ICommand LaunchCommand => new RelayCommand(o => RequestLaunch?.Invoke(), o => PlayButtonEnabled);
        
        //Events
        public event Action RequestLaunch;

        public PlayViewModel()
        {
            
        }
        
        //Getters and Setters
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
                    OnPropertyChanged(nameof(PlayButtonEnabled)); 
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