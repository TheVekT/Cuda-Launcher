// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Launcher.Infrastructure.Localization.Models;

public class LanguageModel(string version, string name, string code, string author)
{
    public string Name { get; init; } = name;
    public string Code { get; init; } = code;

    public string Author { get; set; } = author;
    public string Version { get; set; } = version;

    public override string ToString()
    {
        return Name;
    }
}

public class LanguageFile
{
    public Dictionary<string, string>? Meta { get; init; }
    public Dictionary<string, string>? Translations { get; init; }
}
