using System.Text.Json.Serialization;

namespace Launcher.Core.Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum IsolationType
    {
        Global,
        Full,
        Partial
    }
}