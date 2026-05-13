using Launcher.Core.Instances.Models;

namespace Launcher.Core.Integrations.Abstractions;

public interface IDiscordService
{
    void Initialize(string clientId);
    void SetMenuPresence();
    void SetPlayingPresence(MinecraftInstance instance);
    void ClearPresence();
}