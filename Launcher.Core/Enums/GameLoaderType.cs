using System.Text.Json.Serialization;

namespace Launcher.Core.Enums
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