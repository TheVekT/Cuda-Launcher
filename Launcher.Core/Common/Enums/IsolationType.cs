using System.Text.Json.Serialization;

namespace Launcher.Core.Common.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum IsolationType
{
    Global,
    Full,
    Partial
}
