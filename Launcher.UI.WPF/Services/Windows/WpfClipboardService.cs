using System.Diagnostics;
using System.Windows;
using Launcher.UI.WPF.Services.Windows.Abstractions;

namespace Launcher.UI.WPF.Services.Windows;

public class WpfClipboardService : IClipboardService
{
    public void SetText(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        try
        {
            Clipboard.SetDataObject(text, true);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ClipboardService] Failed to set text: {ex.Message}");
        }
    }
}