using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Launcher.UI.WPF.Messages;
using Launcher.UI.WPF.Services.Windows.Abstractions;

namespace Launcher.UI.WPF.ViewModels.Game;

public partial class CrashViewModel(
    IClipboardService clipboardService,
    int exitCode,
    string stackTrace,
    string? crashReportFilePath)
    : ObservableObject
{
    // ReSharper disable once UnusedMember.Local
    private readonly string? _crashReportFilePath = crashReportFilePath;
    
    [ObservableProperty]
    private int _exitCode = exitCode;
    [ObservableProperty]
    private string _stackTrace = stackTrace;
    [ObservableProperty]
    private string? _crashReportFile = crashReportFilePath != null ? Path.GetFileName(crashReportFilePath) : null;


    [RelayCommand]
    private void CopyStackTrace() =>
        clipboardService.SetText(StackTrace);
    
    [RelayCommand]
    private void CloseSelf() =>
        WeakReferenceMessenger.Default.Send(new CloseOverlayMessage());
}