using System.Windows.Data;
using System.Windows.Markup;
using Launcher.UI.WPF.Services.Customization;

namespace Launcher.UI.WPF.Helpers.Localization;

public class LocExtension : MarkupExtension
{
    public LocKey? Key { get; set; }
    public string? RawKey { get; set; }
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public string? StringFormat { get; set; }

    public LocExtension() { }

    public LocExtension(LocKey key)
    {
        Key = key;
    }

    public LocExtension(string rawKey)
    {
        RawKey = rawKey;
    }

    public override object ProvideValue(IServiceProvider? serviceProvider)
    {
        string? keyString = Key.HasValue ? Key.Value.ToKeyString() : RawKey;
        if (string.IsNullOrEmpty(keyString))
        {
            return string.Empty;
        }

        var binding = new Binding($"[{keyString}]")
        {
            Source = LocalizationService.Instance,
            Mode = BindingMode.OneWay,
            StringFormat = StringFormat
        };

        if (serviceProvider == null)
        {
            return binding;
        }

        return binding.ProvideValue(serviceProvider);
    }
}