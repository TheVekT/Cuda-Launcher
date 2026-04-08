using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

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
    
    private readonly Action _onHide;
    private readonly Action? _onCancel;
    
    public ProgressVM(Action onHide, Action? onCancel = null)
    {
        _onHide = onHide;
        _onCancel = onCancel;
        
        IsCancellable = _onCancel != null;
    }
    
    [RelayCommand]
    private void Hide()
    {
        _onHide?.Invoke();
    }
    
    [RelayCommand]
    private void Cancel()
    {
        _onCancel?.Invoke();
        _onHide?.Invoke(); 
    }
    
    public void Report(double value)
    {
        ProgressValue = value;
        ProgressText = $"{Math.Round(value, 1)}%";
    }
}
