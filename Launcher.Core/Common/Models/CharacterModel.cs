using System.Text.Json.Serialization;

namespace Launcher.Core.Common.Models;

public class CharacterModel
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonPropertyName("skinFileName")]
    public string SkinFileName { get; set; }

    [JsonPropertyName("capeId")] 
    public string? CapeId { get; set; } = null;
    
    [JsonPropertyName("skinVariant")]
    public string SkinVariant { get; set; } = "classic";
    
}