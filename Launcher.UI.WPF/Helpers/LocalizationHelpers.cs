using System.ComponentModel;
using Launcher.UI.WPF.Services;

namespace Launcher.UI.WPF.Helpers;

public class DynamicTranslation : INotifyPropertyChanged
{
    private string _key;
    private object[] _args;
    
    public string Value
    {
        get
        {
            string localizedString = LocalizationService.Instance[_key];
                
            if (_args != null && _args.Length > 0)
            {
                try 
                { 
                    return string.Format(localizedString, _args); 
                }
                catch 
                { 
                    return localizedString;
                }
            }
                
            return localizedString;
        }
    }

    public DynamicTranslation(string key, params object[] args)
    {
        _key = key;
        _args = args;
        
        PropertyChangedEventManager.AddHandler(
            LocalizationService.Instance, 
            OnLocalizationChanged, 
            string.Empty);
    }
    
    private void OnLocalizationChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "Item[]")
        {
            OnPropertyChanged(nameof(Value));
        }
    }

    public void Update(string key, params object[] args)
    {
        _key = key;
        _args = args;
        OnPropertyChanged(nameof(Value)); 
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}