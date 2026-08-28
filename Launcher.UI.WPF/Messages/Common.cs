namespace Launcher.UI.WPF.Messages;

public record CloseOverlayMessage;
public record CloseNotificationMessage(Guid MessageId);
public record ThemeImportedMessage;
public record ThemeChangedMessage(string ThemeFileName);
public record LanguageImportedMessage;
public record OverlayBlinkMessage;
public record LauncherVisibilityMessage(bool IsVisible);