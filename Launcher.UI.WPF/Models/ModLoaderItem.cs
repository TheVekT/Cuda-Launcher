namespace Launcher.UI.WPF.Models;

public record ModLoaderItem(string Name, string Icon)
{
    public override string ToString() => Name;
}