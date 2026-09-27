using System.ComponentModel;

namespace Launcher.Infrastructure.Config.Abstractions;

public interface ISettingsService
{
    void Initialize(INotifyPropertyChanged store);
    T? GetPropertyValue<TStore, T>(string propertyName, T? defaultValue = default);
    T? GetPropertyValue<T>(Type storeType, string propertyName, T? defaultValue = default);
    T? GetPropertyValue<T>(string storeName, string propertyName, T? defaultValue = default);
    T? GetPropertyValue<T>(INotifyPropertyChanged store, string propertyName);
}