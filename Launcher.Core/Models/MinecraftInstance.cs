using System;
using System.Text.Json.Serialization;

namespace Launcher.Core.Models
{
    public class MinecraftInstance
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        public string Name { get; set; }
        public string IconPath { get; set; }
        
        public string GameVersion { get; set; }   // 1.20.1
        public string LoaderVersion { get; set; } // 47.1.0 (для Vanilla будет null или пусто)
        
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public GameLoaderType LoaderType { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public IsolationType IsolationType { get; set; }
        
        public PartialIsolationSettings PartialSettings { get; set; } = new();

        public DateTime? LastPlayedDate { get; set; }
        public MinecraftInstance() { }
    }

    public class PartialIsolationSettings
    {
        public bool IsModsUnique { get; set; } = true; 
        
        public bool IsConfigUnique { get; set; } = false; 
        public bool IsSavesUnique { get; set; } = false;
        public bool IsResourcePacksUnique { get; set; } = false;
    }
}