// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable PropertyCanBeMadeInitOnly.Global
namespace Launcher.Infrastructure.Customization.Models;

public class ThemeModel
{
    public string Name { get; init; } = "Unknown";
    public string Author { get; set; } = "Unknown";
    public string Description { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";

    public string ZipPath { get; set; } = string.Empty;
    public string XamlPath { get; set; } = string.Empty;
    public string? BannerPath { get; set; }
}
