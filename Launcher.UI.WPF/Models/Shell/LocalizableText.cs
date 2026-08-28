using Launcher.UI.WPF.Helpers.Localization;
using Launcher.UI.WPF.Services.Customization;

namespace Launcher.UI.WPF.Models.Shell;

public readonly struct LocalizableText
{
    private readonly string? _rawText;
    private readonly LocKey _translationKey;
    private readonly object[]? _formatArgs;

    public LocalizableText(string text)
    {
        _rawText = text;
        _translationKey = LocKey.None;
        _formatArgs = null;
    }

    public LocalizableText(LocKey key, params object[] formatArgs)
    {
        _rawText = null;
        _translationKey = key;
        _formatArgs = formatArgs;
    }

    public static implicit operator LocalizableText(string text) => new(text);

    public static LocalizableText Key(LocKey key, params object[] args) => new(key, args);

    public string Resolve()
    {
        if (_translationKey != LocKey.None)
        {
            string format = LocalizationService.Instance[_translationKey];
            return _formatArgs is { Length: > 0 } ? string.Format(format, _formatArgs) : format;
        }

        return _rawText ?? string.Empty;
    }
}