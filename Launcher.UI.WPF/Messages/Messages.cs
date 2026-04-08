using Launcher.UI.WPF.Models;

namespace Launcher.UI.WPF.Messages;

public record ThemeImportedMessage();
public record ThemeChangedMessage(string ThemeFileName);
public record LanguageImportedMessage();
public record OverlayBlinkMessage();
public record LauncherVisibilityMessage(bool IsVisible);
public record LaunchGameRequestMessage();
public record CloseOverlayMessage();