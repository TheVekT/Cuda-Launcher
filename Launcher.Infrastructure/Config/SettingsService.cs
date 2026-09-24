using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Launcher.Core.Config.Abstractions;
using Launcher.Infrastructure.Config.Abstractions;
using Launcher.Infrastructure.Config.Models;

namespace Launcher.Infrastructure.Config;

public class SettingsService : ISettingsService
{
    private readonly string _settingsFilePath;
    
    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private readonly ILauncherPathsService _pathsService;
    
    private readonly HashSet<INotifyPropertyChanged> _registeredStores = new ();
    
    private Dictionary<string, Dictionary<string, JsonElement>>? _cachedJsonData;
    
    private CancellationTokenSource? _debounceCts;

    public SettingsService(ILauncherPathsService pathsService)
    {
        _pathsService = pathsService;
        Directory.CreateDirectory(_pathsService.UserDataDirectory);
        _settingsFilePath = Path.Combine(_pathsService.UserDataDirectory, "settings.json");
        LoadRawJson();
    }

    private void LoadRawJson()
    {
        if (!File.Exists(_settingsFilePath))
        {
            _cachedJsonData = new Dictionary<string, Dictionary<string, JsonElement>>();
            return;
        }
        
        try
        {
            string json = File.ReadAllText(_settingsFilePath);
            _cachedJsonData = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, JsonElement>>>(json) 
                              ?? new Dictionary<string, Dictionary<string, JsonElement>>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SettingsService] Error reading global json : {ex.Message}");
            _cachedJsonData = new Dictionary<string, Dictionary<string, JsonElement>>();
        }
    }

    public void Initialize(INotifyPropertyChanged store)
    {
        _registeredStores.Add(store);
        InjectStoreData(store);
        store.PropertyChanged += OnStorePropertyChanged;
    }
    
    public T? GetPropertyValue<TStore, T>(string propertyName, T? defaultValue = default)
    {
        return GetPropertyValue<T>(typeof(TStore), propertyName, defaultValue);
    }

    public T? GetPropertyValue<T>(Type storeType, string propertyName, T? defaultValue = default)
    {
        return GetPropertyValue<T>(storeType.Name, propertyName, defaultValue);
    }

    public T? GetPropertyValue<T>(INotifyPropertyChanged store, string propertyName)
    {
        return GetPropertyValue<T>(store.GetType(), propertyName);
    }

    public T? GetPropertyValue<T>(string storeName, string propertyName, T? defaultValue = default)
    {
        var registeredStore = _registeredStores.FirstOrDefault(s => s.GetType().Name == storeName);
        if (registeredStore != null)
        {
            var prop = registeredStore.GetType().GetProperty(propertyName);
            if (prop != null && Attribute.IsDefined(prop, typeof(SettingPropertyAttribute)))
            {
                var val = prop.GetValue(registeredStore);
                if (val is T typedVal)
                    return typedVal;
            }
        }

        if (_cachedJsonData == null)
            LoadRawJson();

        if (_cachedJsonData != null &&
            _cachedJsonData.TryGetValue(storeName, out var storeData) &&
            storeData.TryGetValue(propertyName, out var jsonElement))
        {
            try
            {
                var deserialized = JsonSerializer.Deserialize(jsonElement.GetRawText(), typeof(T));
                if (deserialized is T typedDeserialized)
                    return typedDeserialized;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SettingsService] Error deserializing {storeName}.{propertyName}: {ex.Message}");
            }
        }

        return defaultValue;
    }

    private void InjectStoreData(INotifyPropertyChanged store)
    {
        if (_cachedJsonData == null) return;
        
        string storeName = store.GetType().Name;
    
        if (!_cachedJsonData.TryGetValue(storeName, out var storeData)) return;

        try
        {
            var properties = store.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in properties)
            {
                if (Attribute.IsDefined(prop, typeof(SettingPropertyAttribute)) && storeData.TryGetValue(prop.Name, out var jsonElement))
                {
                    object? value = JsonSerializer.Deserialize(jsonElement.GetRawText(), prop.PropertyType);
                    if (value != null)
                        prop.SetValue(store, value);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SettingsService] Error injecting in {storeName}: {ex.Message}");
        }
    }

    private void OnStorePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName)) return;

        if (sender is INotifyPropertyChanged store)
        {
            var prop = store.GetType().GetProperty(e.PropertyName);
            if (prop != null && Attribute.IsDefined(prop, typeof(SettingPropertyAttribute)))
                _ = TriggerSavesAsync();
        }
    }

    private async Task TriggerSavesAsync()
    {
        if (_debounceCts != null)
        {
            await _debounceCts.CancelAsync();
            _debounceCts.Dispose();
        }
    
        _debounceCts = new CancellationTokenSource();
        var token = _debounceCts.Token;

        try
        {
            await Task.Delay(500, token); 
        
            if (!token.IsCancellationRequested)
            {
                PerformSave();
            }
        }
        catch (TaskCanceledException) { }
    }

    private void PerformSave()
    {
        try
        {
            var masterDict = new Dictionary<string, Dictionary<string, object?>>();
            foreach (var store in _registeredStores)
            {
                string storeName = store.GetType().Name;
                var storeSettings = new Dictionary<string, object?>();
                var properties = store.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

                foreach (var prop in properties)
                {
                    if (Attribute.IsDefined(prop, typeof(SettingPropertyAttribute)))
                    {
                        storeSettings[prop.Name] = prop.GetValue(store);
                    }
                }
                
                if (storeSettings.Count > 0)
                {
                    masterDict[storeName] = storeSettings;
                }
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(masterDict, options);
            File.WriteAllText(_settingsFilePath, json);
            Console.WriteLine("[SettingsService] Global settings successfully saved!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SettingsService] Saving error: {ex.Message}");
        }
    }
}
