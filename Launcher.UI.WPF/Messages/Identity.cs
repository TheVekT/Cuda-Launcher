using Launcher.Core.Identity.Models;

namespace Launcher.UI.WPF.Messages;

public record AccountLoggedMessage(UserAccount User);

public record SkinApplyResult(
    bool IsSkinApplied,
    bool IsCapeApplied,
    string? SkinErrorMessage = null,
    string? CapeErrorMessage = null)
{
    public bool IsFullySuccessful => IsSkinApplied && IsCapeApplied;
    public bool IsPartiallySuccessful => IsSkinApplied || IsCapeApplied;
}