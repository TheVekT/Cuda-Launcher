using System.Text.Json.Serialization;


namespace Launcher.Core.Identity.Models;

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