
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
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
    
    public ICommand ConfirmCommand { get; }
    public ICommand CancelCommand { get; }
    
    public Task<bool> WaitAsync() => _tcs.Task;
    

    public ConfirmVM(string title, string message, ConfirmButtons buttons = ConfirmButtons.Confirm)
    {
        Title = title;
        Message = message;
        Buttons = buttons;
        
        ConfirmCommand = new RelayCommand(o => _tcs.TrySetResult(true));
        CancelCommand = new RelayCommand(o => _tcs.TrySetResult(false));
    }
    
}
