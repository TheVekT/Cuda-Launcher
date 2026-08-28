using Launcher.Core.Instances.Models;

namespace Launcher.UI.WPF.Services.Shell.Abstractions;

public interface IImportOrchestratorService
{
    Task<int> ProcessDroppedFilesAsync(
        string[] files, 
        MinecraftInstance currentInstance, 
        IProgress<(double Percent, string FileName)> progress, 
        CancellationToken token);
}