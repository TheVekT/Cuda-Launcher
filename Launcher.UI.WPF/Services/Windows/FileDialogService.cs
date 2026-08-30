using Launcher.UI.WPF.Services.Windows.Abstractions;
using Microsoft.Win32;

namespace Launcher.UI.WPF.Services.Windows;

public class WpfFileDialogService : IFileDialogService
{
    public string? OpenFile(string filter = "All Files|*.*", string title = "Open File")
    {
        var dialog = new OpenFileDialog
        {
            Filter = filter,
            Title = title,
            Multiselect = false
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string[]? OpenMultipleFiles(string filter = "All Files|*.*", string title = "Open Files")
    {
        var dialog = new OpenFileDialog
        {
            Filter = filter,
            Title = title,
            Multiselect = true
        };

        return dialog.ShowDialog() == true ? dialog.FileNames : null;
    }

    public string? SaveFile(string defaultName = "", string filter = "All Files|*.*", string title = "Save File")
    {
        var dialog = new SaveFileDialog
        {
            FileName = defaultName,
            Filter = filter,
            Title = title
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? OpenFolder(string title = "Select Folder")
    {
        var dialog = new OpenFolderDialog
        {
            Title = title
        };
        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }
}