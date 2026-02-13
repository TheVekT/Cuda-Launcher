
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Launcher.UI.WPF.Helpers;

namespace Launcher.UI.WPF.ViewModels
{
    public class ConfirmVM : INotifyPropertyChanged
    {
        private readonly TaskCompletionSource<bool> _tcs = new();
        private string _title;
        private string _message;
        
        public ICommand ConfirmCommand { get; }
        public ICommand CancelCommand { get; }
        
        public Task<bool> WaitAsync() => _tcs.Task;
        

        public ConfirmVM(string title, string message)
        {
            Title = title;
            Message = message;
            
            ConfirmCommand = new RelayCommand(o => _tcs.TrySetResult(true));
            CancelCommand = new RelayCommand(o => _tcs.TrySetResult(false));
        }
        

        
        
        //Getters and setters
        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                OnPropertyChanged(nameof(Title));
            }
        }
        
        public string Message
        {
            get => _message;
            set
            {
                _message = value;
                OnPropertyChanged(nameof(Message));
            }
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}