using System.Text.Json.Serialization;

namespace Launcher.Core.Identity.Models;

public class MojangCape
{
    [JsonPropertyName("id")] 
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("state")] 
    public string State { get; set; } = string.Empty;
    
    [JsonPropertyName("url")] 
    public string Url { get; set; } = string.Empty;
    
    [JsonPropertyName("alias")] 
    public string Alias { get; set; } = string.Empty;
}