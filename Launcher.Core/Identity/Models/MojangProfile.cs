using System.Text.Json.Serialization;

namespace Launcher.Core.Identity.Models;

public class MojangProfile
{
    [JsonPropertyName("id")] 
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("name")] 
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("skins")] 
    public List<MojangSkin> Skins { get; set; } = new();
    
    [JsonPropertyName("capes")] 
    public List<MojangCape> Capes { get; set; } = new();
}