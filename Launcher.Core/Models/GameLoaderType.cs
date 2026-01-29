using System.Text.Json.Serialization;

namespace Launcher.Core.Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum GameLoaderType
    {
        Vanilla,
        Forge,
        NeoForge,
        Fabric,
        Quilt
    }
}