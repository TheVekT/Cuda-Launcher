namespace Launcher.UI.WPF.Models.Instances;

public record ModLoaderItem(string Name, string Icon)
{
    public override string ToString() => Name;
}
