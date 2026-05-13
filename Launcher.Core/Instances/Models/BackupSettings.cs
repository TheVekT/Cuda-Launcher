using System.Text.Json.Serialization;

namespace Launcher.Core.Instances.Models;

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
