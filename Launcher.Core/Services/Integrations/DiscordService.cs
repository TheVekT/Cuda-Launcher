using System;
using DiscordRPC;
using Launcher.Core.Models;

namespace Launcher.Core.Services
{
    public interface IDiscordService
    {
        void Initialize(string clientId);
        void SetMenuPresence();
        void SetPlayingPresence(MinecraftInstance instance);
        void ClearPresence();
    }

    public class DiscordService : IDiscordService, IDisposable
    {
        private DiscordRpcClient _client;
        private bool _isInitialized;

        public void Initialize(string clientId)
        {
            if (_isInitialized) return;

            _client = new DiscordRpcClient(clientId);
            
            _client.Initialize();
            _isInitialized = true;
            
            SetMenuPresence(); // Сразу ставим статус меню при запуске
        }

        public void SetMenuPresence()
        {
            if (!_isInitialized) return;

            _client.SetPresence(new RichPresence()
            {
                Details = "Main Menu",
                Assets = new Assets()
                {
                    LargeImageKey = "logo",
                    LargeImageText = "MineLauncher"
                },
                Timestamps = Timestamps.Now // Запускает счетчик времени ("Прошло: 00:01")
            });
        }

        public void SetPlayingPresence(MinecraftInstance instance)
        {
            if (!_isInitialized || instance == null) return;

            _client.SetPresence(new RichPresence()
            {
                Details = $"Playing: {instance.Name}",
                State = $"Version: {instance.GameVersion}  |  {instance.LoaderType}",
                Assets = new Assets()
                {
                    LargeImageKey = "logo", 
                    LargeImageText = "MineLauncher",
                    SmallImageKey = "playing", // Маленькая иконка (например, геймпад)
                    SmallImageText = "In Game"
                },
                Timestamps = Timestamps.Now
            });
        }

        public void ClearPresence()
        {
            if (_isInitialized) _client.ClearPresence();
        }

        public void Dispose()
        {
            if (_client != null)
            {
                _client.Dispose();
                _isInitialized = false;
            }
        }
    }
}