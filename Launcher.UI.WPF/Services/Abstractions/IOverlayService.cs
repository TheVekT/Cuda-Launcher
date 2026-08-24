namespace Launcher.UI.WPF.Services.Abstractions;

public interface IOverlayService
{
    void Show(object viewModel);
    void Close();
    void SetClosable(bool isClosable);
    
    void RegisterOverlaySetter(Action<object?> updateOverlayAction);
}