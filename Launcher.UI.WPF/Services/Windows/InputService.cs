using System.Windows.Input;
using Launcher.UI.WPF.Services.Windows.Abstractions;

namespace Launcher.UI.WPF.Services.Windows;

public class InputService : IInputService
{
    public bool IsShiftPressed => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift); 
}