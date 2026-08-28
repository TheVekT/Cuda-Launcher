using CommunityToolkit.Mvvm.Messaging;
using Launcher.UI.WPF.Messages;
using Launcher.UI.WPF.Services.Shell.Abstractions;

namespace Launcher.UI.WPF.Services.Shell;

public class OverlayService : IOverlayService
{
    private Action<object?>? _updateOverlayAction;

    private bool _isClosable = true;

    public void RegisterOverlaySetter(Action<object?> updateOverlayAction)
    {
        _updateOverlayAction = updateOverlayAction;
    }

    public void Show(object viewModel)
    {
        _updateOverlayAction?.Invoke(viewModel);
    }

    public void Close()
    {
        if (!_isClosable) 
        {
            WeakReferenceMessenger.Default.Send(new OverlayBlinkMessage());
            return;
        }
    
        _updateOverlayAction?.Invoke(null);
        _isClosable = true;
    }

    public void SetClosable(bool isClosable)
    {
        _isClosable = isClosable;
    }
}