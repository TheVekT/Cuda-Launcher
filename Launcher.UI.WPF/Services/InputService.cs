using System.Windows.Input;
using Launcher.Core.Services.IO;

namespace Launcher.UI.WPF.Services;

public class InputService : IInputService
{
    public bool IsShiftPressed => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift); 
}