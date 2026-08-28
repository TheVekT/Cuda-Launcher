namespace Launcher.UI.WPF.Services.Abstractions;

public interface IFileDialogService
{
    string? OpenFile(string filter = "All Files|*.*", string title = "Open File");
    
    string[]? OpenMultipleFiles(string filter = "All Files|*.*", string title = "Open Files");
    
    string? SaveFile(string defaultName = "", string filter = "All Files|*.*", string title = "Save File");
    
    string? OpenFolder(string title = "Select Folder");
}