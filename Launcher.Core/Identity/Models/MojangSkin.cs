using System.Text.Json.Serialization;


namespace Launcher.Core.Identity.Models;

public class MojangSkin
{
    [JsonPropertyName("id")] 
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("state")] 
    public string State { get; set; } = string.Empty;
    
    [JsonPropertyName("url")] 
    public string Url { get; set; } = string.Empty;
    
    [JsonPropertyName("variant")] 
    public string Variant { get; set; } = string.Empty;
}