using System.Text.Json.Serialization;

namespace Launcher.Core.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum IsolationType
{
    Global,
    Full,
    Partial
}
