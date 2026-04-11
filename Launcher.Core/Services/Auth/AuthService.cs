using CmlLib.Core.Auth;
using CmlLib.Core.Auth.Microsoft;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using Launcher.Core.Messages;
using Launcher.Core.Models;

namespace Launcher.Core.Services.Auth;

public interface IAuthService
{
    Task<UserAccount> LoginWithMicrosoftAsync();
    UserAccount LoginOffline(string nickname);
    Task<UserAccount> ValidateAndRefreshAccountAsync(UserAccount account);
}

public class AuthService : IAuthService
{
    private readonly JELoginHandler _loginHandler;
    private readonly HttpClient _httpClient;

    public AuthService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _loginHandler = JELoginHandlerBuilder.BuildDefault();
    }
    
    public async Task<UserAccount> LoginWithMicrosoftAsync()
    {
        try 
        {
            var session = await _loginHandler.AuthenticateInteractively();
            var user = new UserAccount(
                session.Username, 
                session.UUID, 
                session.AccessToken, 
                isOffline: false);
            return user;
        }
        catch (Exception ex)
        {
            throw new Exception($"Microsoft Login failed: {ex.Message}");
        }
    }

    public UserAccount LoginOffline(string nickname)
    {
        var offlineSession = MSession.CreateOfflineSession(nickname);
        return new UserAccount(offlineSession.Username, offlineSession.UUID, offlineSession.AccessToken, isOffline: true);
    }

    public async Task<UserAccount> ValidateAndRefreshAccountAsync(UserAccount account)
    {
        if (account == null) return null;
        if (account.IsOffline) return account;

        if (string.IsNullOrEmpty(account.AccessToken))
        {
            throw new UnauthorizedAccessException("Token is missing or decryption failed.");
        }
        
        try
        {
            Debug.WriteLine($"[Auth] Validating token for {account.Username}...");

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", account.AccessToken);
            
            var response = await _httpClient.GetAsync("https://api.minecraftservices.com/minecraft/profile");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;
                
                if (root.TryGetProperty("name", out var nameElement))
                {
                    account.Username = nameElement.GetString();
                }
                Debug.WriteLine($"[Auth] Token valid. User: {account.Username}");
                return account;
            }
            else
            {
                // Сервер вернул ошибку (токен протух, обычно они живут 24 часа).
                throw new Exception("Minecraft Access Token has expired. Re-login required.");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Auth Error] {ex}");
            throw new Exception($"Session validation failed: {ex.Message}");
        }
    }
}
