using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Launcher.Core.Models;

namespace Launcher.Core.Services.IO;

public interface ISettingsService
{
    void Initialize(INotifyPropertyChanged store);
}

public class SettingsService : ISettingsService
{
    private readonly string _settingsFilePath;
    
    // Храним ссылки на все сторы, которые попросили их сохранять
    private readonly HashSet<INotifyPropertyChanged> _registeredStores = new HashSet<INotifyPropertyChanged>();
    
    // Кэшируем сырой JSON при старте, чтобы раздавать данные сторам по мере их инициализации
    private Dictionary<string, Dictionary<string, JsonElement>> _cachedJsonData;
    
    private CancellationTokenSource _debounceCts;

    public SettingsService()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string userDataDir = Path.Combine(baseDir, "Data", "UserData");
        Directory.CreateDirectory(userDataDir);
        _settingsFilePath = Path.Combine(userDataDir, "settings.json");

        // Читаем файл один раз при запуске сервиса
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
            // Десериализуем как Словарь(ИмяСтора -> Словарь(ИмяСвойства -> Значение))
            _cachedJsonData = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, JsonElement>>>(json) 
                              ?? new Dictionary<string, Dictionary<string, JsonElement>>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SettingsService] Ошибка чтения глобального JSON: {ex.Message}");
            _cachedJsonData = new Dictionary<string, Dictionary<string, JsonElement>>();
        }
    }

    public void Initialize(INotifyPropertyChanged store)
    {
        // Защита от повторной инициализации одного и того же стора
        if (store == null || _registeredStores.Contains(store)) return;

        _registeredStores.Add(store);
        
        // 1. Внедряем данные ИМЕННО для этого стора
        InjectStoreData(store);

        // 2. Подписываемся на изменения
        store.PropertyChanged += OnStorePropertyChanged;
    }

    private void InjectStoreData(INotifyPropertyChanged store)
    {
        string storeName = store.GetType().Name; // Например "SettingsStore" или "LoginStore"

        // Если в кэше нет секции для этого стора, значит файл пустой или стор новый
        if (!_cachedJsonData.TryGetValue(storeName, out var storeData)) return;

        try
        {
            var properties = store.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in properties)
            {
                if (Attribute.IsDefined(prop, typeof(SettingPropertyAttribute)) && storeData.TryGetValue(prop.Name, out var jsonElement))
                {
                    object value = JsonSerializer.Deserialize(jsonElement.GetRawText(), prop.PropertyType);
                    if (value != null)
                    {
                        prop.SetValue(store, value);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SettingsService] Ошибка инъекции в {storeName}: {ex.Message}");
        }
    }

    private void OnStorePropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (sender is INotifyPropertyChanged store)
        {
            var prop = store.GetType().GetProperty(e.PropertyName);
            if (prop != null && Attribute.IsDefined(prop, typeof(SettingPropertyAttribute)))
            {
                // Любое изменение в ЛЮБОМ сторе триггерит глобальное сохранение
                TriggerSave();
            }
        }
    }

    private async void TriggerSave()
    {
        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();
        var token = _debounceCts.Token;

        try
        {
            await Task.Delay(500, token); // Единый кулдаун на все сторы
            
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
            // Главный словарь, который станет JSON'ом
            var masterDict = new Dictionary<string, Dictionary<string, object>>();

            // Пробегаемся по всем сторам, которые мы отслеживаем
            foreach (var store in _registeredStores)
            {
                string storeName = store.GetType().Name;
                var storeSettings = new Dictionary<string, object>();
                var properties = store.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

                foreach (var prop in properties)
                {
                    if (Attribute.IsDefined(prop, typeof(SettingPropertyAttribute)))
                    {
                        storeSettings[prop.Name] = prop.GetValue(store);
                    }
                }

                // Если в сторе есть хотя бы одно свойство с атрибутом — добавляем его в файл
                if (storeSettings.Count > 0)
                {
                    masterDict[storeName] = storeSettings;
                }
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(masterDict, options);
            File.WriteAllText(_settingsFilePath, json);
            Console.WriteLine("[SettingsService] Глобальные настройки успешно сохранены!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SettingsService] Ошибка глобального сохранения: {ex.Message}");
        }
    }
}
