using System.Text.Json;
using System.Text.Json.Serialization;

namespace Launcher.Updater.Models;

public class AppManifest
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("files")]
    public JsonElement FilesElement { get; set; }

    public HashSet<string> GetFilePaths()
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (FilesElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in FilesElement.EnumerateObject())
            {
                paths.Add(prop.Name);
            }
        }
        else if (FilesElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in FilesElement.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    string? str = item.GetString();
                    if (!string.IsNullOrWhiteSpace(str))
                        paths.Add(str);
                }
            }
        }

        return paths;
    }

    public Dictionary<string, string> GetFilesWithHashes()
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (FilesElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in FilesElement.EnumerateObject())
            {
                result[prop.Name] = prop.Value.GetString() ?? string.Empty;
            }
        }
        else if (FilesElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in FilesElement.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    string? str = item.GetString();
                    if (!string.IsNullOrWhiteSpace(str))
                        result[str] = string.Empty;
                }
            }
        }

        return result;
    }
}