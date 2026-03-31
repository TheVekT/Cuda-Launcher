using System;
using System.ComponentModel;
using System.Windows.Input;
using Launcher.UI.WPF.Helpers; // Путь к твоему RelayCommand

namespace Launcher.UI.WPF.ViewModels.Settings;

public class ProgressVM : INotifyPropertyChanged, IProgress<double> // Предполагаю, что у тебя есть базовый класс с INotifyPropertyChanged
{
    private string _title;
    
    public string Title 
    { 
        get => _title; 
        set { _title = value; OnPropertyChanged(nameof(Title)); } 
    }

    private string _message;
    public string Message 
    { 
        get => _message; 
        set { _message = value; OnPropertyChanged(nameof(Message)); } 
    }

    private double _progressValue;
    public double ProgressValue 
    { 
        get => _progressValue; 
        set { _progressValue = value; OnPropertyChanged(nameof(ProgressValue)); } 
    }

    private string _progressText;
    public string ProgressText 
    { 
        get => _progressText; 
        set { _progressText = value; OnPropertyChanged(nameof(ProgressText)); } 
    }

    private bool _isIndeterminate;
    public bool IsIndeterminate 
    { 
        get => _isIndeterminate; 
        set { _isIndeterminate = value; OnPropertyChanged(nameof(IsIndeterminate) ); } 
    }

    private bool _isCancellable;
    public bool IsCancellable 
    { 
        get => _isCancellable; 
        set { _isCancellable = value; OnPropertyChanged(nameof(IsCancellable)); } 
    }

    public ICommand CancelCommand { get; }
    public ICommand HideCommand { get; }

    public ProgressVM(Action onHide, Action onCancel = null)
    {
        HideCommand = new RelayCommand(_ => onHide?.Invoke());

        if (onCancel != null)
        {
            IsCancellable = true;
            CancelCommand = new RelayCommand(_ => 
            {
                onCancel();
                onHide?.Invoke(); 
            });
        }
        else
        {
            IsCancellable = false;
        }
    }
    
    public void Report(double value)
    {
        ProgressValue = value;
        ProgressText = $"{Math.Round(value, 1)}%";
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
