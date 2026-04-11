using System.Text.Json.Serialization;

namespace Launcher.Core.Models;

public class MojangProfileInfo
{
    [JsonPropertyName("id")] 
    public string Id { get; set; }
    
    [JsonPropertyName("name")] 
    public string Name { get; set; }
    
    [JsonPropertyName("skins")] 
    public List<MojangSkin> Skins { get; set; } = new();
    
    [JsonPropertyName("capes")] 
    public List<MojangCape> Capes { get; set; } = new();
}

public class MojangSkin
{
    [JsonPropertyName("id")] 
    public string Id { get; set; }
    
    [JsonPropertyName("state")] 
    public string State { get; set; }
    
    [JsonPropertyName("url")] 
    public string Url { get; set; }
    
    [JsonPropertyName("variant")] 
    public string Variant { get; set; }
}

public class MojangCape
{
    [JsonPropertyName("id")] 
    public string Id { get; set; }
    
    [JsonPropertyName("state")] 
    public string State { get; set; } 
    
    [JsonPropertyName("url")] 
    public string Url { get; set; }
    
    [JsonPropertyName("alias")] 
    public string Alias { get; set; }
}