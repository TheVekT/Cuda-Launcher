using System.Text.Json.Serialization;

namespace Launcher.Core.Identity.Models;

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