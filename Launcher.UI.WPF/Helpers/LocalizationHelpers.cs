using System.ComponentModel;
using Launcher.UI.WPF.Services;

namespace Launcher.UI.WPF.Helpers;

public class DynamicTranslation : INotifyPropertyChanged
{
    private string _key;
    private object[] _args;

    // XAML будет биндиться именно к этому свойству
    public string Value
    {
        get
        {
            // Берём свежий перевод из сервиса
            string localizedString = LocalizationService.Instance[_key];
                
            // Если есть аргументы (например, версия игры), подставляем их
            if (_args != null && _args.Length > 0)
            {
                try 
                { 
                    return string.Format(localizedString, _args); 
                }
                catch 
                { 
                    return localizedString; // Защита от кривого формата в JSON
                }
            }
                
            return localizedString;
        }
    }

    public DynamicTranslation(string key, params object[] args)
    {
        _key = key;
        _args = args;

        // Самая важная магия: подписываемся на смену языка навсегда
        LocalizationService.Instance.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == "Item[]")
            {
                // Говорим UI, что свойство Value изменилось, пусть перечитает
                OnPropertyChanged(nameof(Value));
            }
        };
    }

    // Метод для изменения текста на лету из ViewModel
    public void Update(string key, params object[] args)
    {
        _key = key;
        _args = args;
        OnPropertyChanged(nameof(Value)); // Дергаем UI
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}