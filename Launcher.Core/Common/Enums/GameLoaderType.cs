using System.Text.Json.Serialization;

namespace Launcher.Core.Common.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GameLoaderType
{
    Vanilla,
    Forge,
    NeoForge,
    Fabric,
    Quilt
}
