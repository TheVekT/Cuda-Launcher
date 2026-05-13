using System.ComponentModel;

namespace Launcher.Core.Config.Abstractions;

public interface ISettingsService
{
    void Initialize(INotifyPropertyChanged store);
}