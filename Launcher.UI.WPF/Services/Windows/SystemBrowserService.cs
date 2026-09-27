using System.Diagnostics;
using Launcher.UI.WPF.Services.Windows.Abstractions;

namespace Launcher.UI.WPF.Services.Windows;

public class SystemBrowserService : IBrowserService
{
    public bool OpenUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SystemBrowserService] Failed to open URL '{url}': {ex.Message}");
            return false;
        }
    }
}