using System.Text.Json.Serialization;

namespace Launcher.Infrastructure.Assets.Models;

public class CharacterModel(string id, string name, string skinFileName, string? capeId, string skinVariant)
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = id;

    [JsonPropertyName("name")]
    public string Name { get; set; } = name;

    [JsonPropertyName("skinFileName")]
    public string SkinFileName { get; set; } = skinFileName;

    [JsonPropertyName("capeId")] 
    public string? CapeId { get; set; } = capeId;

    [JsonPropertyName("skinVariant")]
    public string SkinVariant { get; set; } = skinVariant;
}