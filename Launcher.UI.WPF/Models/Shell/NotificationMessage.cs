using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Launcher.UI.WPF.Helpers.Enums;
using Launcher.UI.WPF.Services.Customization;

namespace Launcher.UI.WPF.Models.Shell;

public class NotificationMessage : ObservableObject
{
    private readonly LocalizableText _rawTitle;
    private readonly LocalizableText _rawMessage;

    public Guid Id { get; } = Guid.NewGuid();
    public NotificationType Type { get; init; }

    public string Title => _rawTitle.Resolve();
    public string Message => _rawMessage.Resolve();

    public NotificationMessage(LocalizableText title, LocalizableText message, NotificationType type)
    {
        _rawTitle = title;
        _rawMessage = message;
        Type = type;
        
        PropertyChangedEventManager.AddHandler(
            LocalizationService.Instance, 
            OnLocalizationChanged, 
            string.Empty);
    }

    private void OnLocalizationChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(Message));
    }
}