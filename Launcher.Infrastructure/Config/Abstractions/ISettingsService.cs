using System.ComponentModel;

namespace Launcher.Infrastructure.Config.Abstractions;

public interface ISettingsService
{
    void Initialize(INotifyPropertyChanged store);
}