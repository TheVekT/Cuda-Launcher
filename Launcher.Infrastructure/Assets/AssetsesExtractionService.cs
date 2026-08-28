using System.Reflection;
using Launcher.Core.Config.Abstractions;
using Launcher.Infrastructure.Assets.Abstractions;

namespace Launcher.Infrastructure.Assets;

public class AssetsesExtractionService(ILauncherPathsService pathsService) : IAssetsExtractionService
{
    private readonly string[] _defaultThemes = ["default-dark.zip", "default-light.zip"];
    private readonly string[] _defaultLangs = ["en-US.json"];
    private readonly string[] _defaultIcons = ["00_Grass_Block_JE7_BE6.png", "fabric-default.png", "forge-default.png", "logo.png", "neoforge-default.png", "optifine-default.png", "quilt-default.png"];
    private readonly string[] _defaultBanners = ["banner-default.jpg"];
    private readonly string[] _defaultWeb = ["skinview.html", "skinview3d.bundle.js"];

    public void EnsureAllBaseAssetsExist()
    {
        RestoreFiles(Path.Combine(pathsService.AssetsDirectory, "Themes"), _defaultThemes);
        
        RestoreFiles(Path.Combine(pathsService.AssetsDirectory, "Languages"), _defaultLangs);
        
        RestoreFiles(Path.Combine(pathsService.AssetsDirectory, "Icons"), _defaultIcons);
        
        RestoreFiles(Path.Combine(pathsService.AssetsDirectory, "Images"), _defaultBanners);
        
        RestoreFiles(Path.Combine(pathsService.AssetsDirectory, "Web"), _defaultWeb);
    }

    private void RestoreFiles(string targetDirectory, string[] fileNames)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var allResources = assembly.GetManifestResourceNames();

        if (!Directory.Exists(targetDirectory))
        {
            Directory.CreateDirectory(targetDirectory);
        }

        foreach (var fileName in fileNames)
        {
            var destPath = Path.Combine(targetDirectory, fileName);
            var resourceName = allResources.FirstOrDefault(r => r.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(resourceName))
            {
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null) continue;

                var fileInfo = new FileInfo(destPath);
                if (!fileInfo.Exists || fileInfo.Length != stream.Length)
                {
                    using var fileStream = File.Create(destPath);
                    stream.CopyTo(fileStream);
                    
                    System.Diagnostics.Debug.WriteLine($"[Assets] Restored or Updated: {fileName}");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[Assets] CRITICAL: Resource '{fileName}' not found!");
            }
        }
    }
}