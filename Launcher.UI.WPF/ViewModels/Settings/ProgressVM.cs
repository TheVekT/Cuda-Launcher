using System;
using System.ComponentModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using Launcher.UI.WPF.Helpers; // Путь к твоему RelayCommand

namespace Launcher.UI.WPF.ViewModels.Settings;

public partial class ProgressVM : ObservableObject, IProgress<double>
{
    [ObservableProperty]
    private string _title;
    [ObservableProperty]
    private string _message;
    [ObservableProperty]
    private double _progressValue;
    [ObservableProperty]
    private string _progressText;
    [ObservableProperty]
    private bool _isIndeterminate;
    [ObservableProperty]
    private bool _isCancellable;

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
}
