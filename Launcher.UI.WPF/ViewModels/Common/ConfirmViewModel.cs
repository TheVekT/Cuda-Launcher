using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Launcher.UI.WPF.Helpers;
using Launcher.UI.WPF.Messages;

namespace Launcher.UI.WPF.ViewModels.Common;

public partial class ConfirmViewModel : ObservableObject, IRecipient<CloseOverlayMessage>
{
    private readonly TaskCompletionSource<bool> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    [ObservableProperty]
    private string _title;
    [ObservableProperty]
    private string _message;
    [ObservableProperty]
    private ConfirmButtons _buttons;
    
    public Task<bool> WaitAsync() => _tcs.Task;
    

    public ConfirmViewModel(string title, string message, ConfirmButtons buttons = ConfirmButtons.Confirm)
    {
        Title = title;
        Message = message;
        Buttons = buttons;
        
        WeakReferenceMessenger.Default.RegisterAll(this);
    }

    public void Receive(CloseOverlayMessage message) =>
        Complete(false);
    
    private void Complete(bool result)
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
        _tcs.TrySetResult(result);
    }

    //Commands
    [RelayCommand]
    private void Confirm() => 
        Complete(true);
    
    [RelayCommand]
    private void Cancel() => 
        Complete(false);
}
