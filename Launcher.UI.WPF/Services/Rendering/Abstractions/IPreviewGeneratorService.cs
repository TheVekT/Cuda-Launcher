namespace Launcher.UI.WPF.Services.Rendering.Abstractions;

public interface IPreviewGeneratorService
{
    string? GenerateCapePreview(string originalCapePath, string capeId);
    Task<string?> Generate3DSkinSnapshotAsync(string skinPath, string? capePath, string characterId, bool isSlim, bool forceRegenerate = false);
}