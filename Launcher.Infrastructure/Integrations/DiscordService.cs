using DiscordRPC;
using Launcher.Core.Instances.Models;
using Launcher.Infrastructure.Integrations.Abstractions;

namespace Launcher.Infrastructure.Integrations;

public class DiscordService : IDiscordService, IDisposable
{
    private DiscordRpcClient? _client;
    private bool _isInitialized;

    public void Initialize(string clientId)
    {
        if (_isInitialized) return;

        _client = new DiscordRpcClient(clientId);
        
        _client.Initialize();
        _isInitialized = true;
        
        SetMenuPresence();
    }

    public void SetMenuPresence()
    {
        if (!_isInitialized) return;

        _client?.SetPresence(new RichPresence()
        {
            Details = "Main Menu",
            Assets = new DiscordRPC.Assets()
            {
                LargeImageKey = "logo",
                LargeImageText = "Cuda Launcher"
            },
            Timestamps = Timestamps.Now
        });
    }

    public void SetPlayingPresence(MinecraftInstance? instance)
    {
        if (!_isInitialized || instance == null) return;

        _client?.SetPresence(new RichPresence
        {
            Details = $"Playing: {instance.Name}",
            State = $"{instance.LoaderType} {instance.GameVersion}",
            Assets = new DiscordRPC.Assets()
            {
                LargeImageKey = "logo", 
                LargeImageText = "Cuda Launcher",
                SmallImageKey = "playing",
                SmallImageText = "In Game"
            },
            Timestamps = Timestamps.Now
        });
    }

    public void ClearPresence()
    {
        if (_isInitialized) _client?.ClearPresence();
    }

    public void Dispose()
    {
        _client?.Dispose();
        _isInitialized = false;
    }
}
