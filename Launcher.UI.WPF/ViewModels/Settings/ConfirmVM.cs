using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Launcher.UI.WPF.Helpers;

namespace Launcher.UI.WPF.ViewModels.Settings;

public partial class ConfirmVM : ObservableObject
{
    private readonly TaskCompletionSource<bool> _tcs = new();
    [ObservableProperty]
    private string _title;
    [ObservableProperty]
    private string _message;
    [ObservableProperty]
    private ConfirmButtons _buttons;
    
    public Task<bool> WaitAsync() => _tcs.Task;
    

    public ConfirmVM(string title, string message, ConfirmButtons buttons = ConfirmButtons.Confirm)
    {
        Title = title;
        Message = message;
        Buttons = buttons;
    }
    
    //Commands
    [RelayCommand]
    private void Confirm() => 
        _tcs.TrySetResult(true);
    
    [RelayCommand]
    private void Cancel() => 
        _tcs.TrySetResult(false);
    
}
