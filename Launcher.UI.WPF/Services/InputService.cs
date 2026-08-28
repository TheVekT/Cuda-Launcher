using System.Windows.Input;
using Launcher.UI.WPF.Services.Abstractions;

namespace Launcher.UI.WPF.Services;

public class InputService : IInputService
{
    public bool IsShiftPressed => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift); 
}