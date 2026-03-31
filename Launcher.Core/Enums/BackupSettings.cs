using System.Text.Json.Serialization;

namespace Launcher.Core.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BackupPolicy
{
    Inherit,
    ForceOn,
    ForceOff
}
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BackupFrequency
{
    Daily,
    Weekly,
    Biweekly,
    Monthly
}
