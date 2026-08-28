using System.Collections.Concurrent;

namespace Launcher.UI.WPF.Helpers.Localization;

public static class LocKeyExtensions
{
    private static readonly ConcurrentDictionary<LocKey, string> KeyCache = new();

    public static string ToKeyString(this LocKey key)
    {
        return KeyCache.GetOrAdd(key, k => k.ToString().Replace('_', '.'));
    }
}
