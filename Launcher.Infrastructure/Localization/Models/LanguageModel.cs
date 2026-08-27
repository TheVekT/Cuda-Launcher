namespace Launcher.Infrastructure.Localization.Models;

public class LanguageModel
{
    public string Name { get; set; } 
    public string Code { get; set; } 
    
    public string Author { get; set; }
    public string Version { get; set; }

    public override string ToString()
    {
        return Name;
    }
}
