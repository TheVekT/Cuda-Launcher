namespace Launcher.UI.WPF.Models.Game;

public record SkinApplyResult(
    bool IsSkinApplied,
    bool IsCapeApplied,
    string? SkinErrorMessage = null,
    string? CapeErrorMessage = null)
{
    public bool IsFullySuccessful => IsSkinApplied && IsCapeApplied;
    public bool IsPartiallySuccessful => IsSkinApplied || IsCapeApplied;
}
