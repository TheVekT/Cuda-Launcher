using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Launcher.UI.WPF.Messages;
using Launcher.UI.WPF.Services.Abstractions;

namespace Launcher.UI.WPF.ViewModels.Game;

public partial class CrashViewModel: ObservableObject
{
    private readonly IClipboardService _clipboardService;
    
    private readonly string _crashReportFilePath;
    
    [ObservableProperty]
    private int _exitCode;
    [ObservableProperty]
    private string _stackTrace;
    [ObservableProperty]
    private string _crashReportFile;
    
    public CrashViewModel(IClipboardService clipboardService, int exitCode, string stackTrace, string crashReportFilePath)
    {
        _clipboardService = clipboardService;
        _exitCode = exitCode;
        _stackTrace = stackTrace;
        _crashReportFilePath = crashReportFilePath;
        _crashReportFile = Path.GetFileName(crashReportFilePath);
    }
    

    [RelayCommand]
    private void CopyStackTrace() =>
        _clipboardService.SetText(StackTrace);
    
    [RelayCommand]
    private void CloseSelf() =>
        WeakReferenceMessenger.Default.Send(new CloseOverlayMessage());
}