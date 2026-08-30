using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text.Json;
using CmlLib.Core.Auth;
using CmlLib.Core.Auth.Microsoft;
using Launcher.Core.Config.Abstractions;
using Launcher.Core.Identity.Abstractions;
using Launcher.Core.Identity.Exceptions;
using Launcher.Core.Identity.Models;

namespace Launcher.Core.Identity;

public class AuthService : IAuthService
{
    private readonly JELoginHandler _loginHandler;
    private readonly HttpClient _httpClient;

    public AuthService(HttpClient httpClient, ILauncherPathsService pathsService)
    {
        _httpClient = httpClient;
        
        string cmlAccountsPath = Path.Combine(pathsService.UserDataDirectory, "cml_accounts.json");
        
        // Using the clean, built-in XboxAuthNet engine without external MSAL providers
        _loginHandler = new JELoginHandlerBuilder()
            .WithAccountManager(cmlAccountsPath)
            .Build();
    }

    public async Task<UserAccount> LoginWithMicrosoftAsync()
    {
        try 
        {
            var authenticator = _loginHandler.CreateAuthenticatorWithNewAccount();
            
            // Native XboxAuthNet embedded browser. Works perfectly with the official ClientID.
            authenticator.AddMicrosoftOAuthForJE(oauth => oauth.Interactive()); 
            authenticator.AddXboxAuthForJE(xbox => xbox.Basic());
            authenticator.AddJEAuthenticator();

            var session = await authenticator.ExecuteForLauncherAsync();

            // Workaround: Explicitly set the Identifier to prevent the AccountManager
            // from filtering out this newly created session.
            if (!string.IsNullOrEmpty(session.UUID))
            {
                authenticator.Context?.SessionStorage.Set("Identifier", session.UUID);
            }

            // Persist session data to cml_accounts.json
            _loginHandler.AccountManager.SaveAccounts();

            var accounts = _loginHandler.AccountManager.GetAccounts();
            Debug.WriteLine($"[Auth] Interactive login completed. Records in cml_accounts.json: {accounts.Count}");

            return new UserAccount(
                session.Username ?? string.Empty, 
                session.UUID ?? string.Empty, 
                session.AccessToken, 
                isOffline: false);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Auth Error] Microsoft Login failed: {ex}");

            if (IsMinecraftNotPurchased(ex))
            {
                throw new MinecraftNotPurchasedException("This Microsoft account does not own Minecraft: Java Edition.", ex);
            }

            throw new Exception($"Microsoft Login failed: {ex.Message}", ex);
        }
    }

    public UserAccount LoginOffline(string nickname)
    {
        string safeNickname = string.IsNullOrWhiteSpace(nickname) ? "Player" : nickname.Trim();
        var offlineSession = MSession.CreateOfflineSession(safeNickname);
    
        return new UserAccount(
            offlineSession.Username ?? safeNickname, 
            offlineSession.UUID ?? Guid.NewGuid().ToString(), 
            offlineSession.AccessToken, 
            isOffline: true);
    }

    public async Task<UserAccount> ValidateAndRefreshAccountAsync(UserAccount account)
    {
        if (account.IsOffline) return account;
        
        if (string.IsNullOrEmpty(account.AccessToken))
            throw new UnauthorizedAccessException("Token is missing.");
        HttpResponseMessage response;
        try
        {
            Debug.WriteLine($"[Auth] Validating token for {account.Username}...");
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.minecraftservices.com/minecraft/profile");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", account.AccessToken);
            
            response = await _httpClient.SendAsync(request);
        }
        catch (Exception ex) when (ex is HttpRequestException or SocketException or TaskCanceledException)
        {
            Debug.WriteLine($"[Auth Warn] Network unreachable. Proceeding with cached credentials for {account.Username}.");
            return account;
        }

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.TryGetProperty("name", out var nameElement))
            {
                account.Username = nameElement.GetString() ?? account.Username;
            }
            Debug.WriteLine($"[Auth] Token is valid. User: {account.Username}");
            return account;
        }
        
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            Debug.WriteLine($"[Auth Error] Account {account.Username} has no Minecraft profile (404 Not Found).");
            throw new MinecraftNotPurchasedException("This Microsoft account does not own Minecraft: Java Edition.");
        }
        
        if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
        {
            Debug.WriteLine("[Auth] Token expired! Starting built-in Silent Refresh...");
            try
            {
                var authenticator = _loginHandler.CreateAuthenticatorWithDefaultAccount();

                authenticator.AddMicrosoftOAuthForJE(oauth => oauth.Silent()); 
                authenticator.AddXboxAuthForJE(xbox => xbox.Basic());
                authenticator.AddJEAuthenticator();

                var newSession = await authenticator.ExecuteForLauncherAsync();

                _loginHandler.AccountManager.SaveAccounts();

                account.AccessToken = newSession.AccessToken;
                account.Username = newSession.Username ?? account.Username;
                account.UUID = newSession.UUID ?? account.UUID;

                Debug.WriteLine("[Auth] Silent Refresh pipeline completed successfully!");
                return account;
            }
            catch (Exception ex) when (ex is HttpRequestException or SocketException or TaskCanceledException)
            {
                Debug.WriteLine($"[Auth Warn] Network unreachable during Silent Refresh. Keeping cached credentials for {account.Username}.");
                return account;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Auth Error] Refresh failed: {ex}");
                if (IsMinecraftNotPurchased(ex))
                {
                    throw new MinecraftNotPurchasedException("This Microsoft account does not own Minecraft: Java Edition.", ex);
                }
                throw new UnauthorizedAccessException("Session expired or cache is empty. Manual Microsoft login required.", ex);
            }
        }

        return account;
    }

    private static bool IsMinecraftNotPurchased(Exception ex)
    {
        Exception? current = ex;
        while (current != null)
        {
            if (current.Message.Contains("NOT_FOUND", StringComparison.OrdinalIgnoreCase) ||
                current.Message.Contains("Not Found", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (current is HttpRequestException httpEx && httpEx.StatusCode == HttpStatusCode.NotFound)
            {
                return true;
            }

            if (current is WebException webEx && webEx.Response is HttpWebResponse webResp && webResp.StatusCode == HttpStatusCode.NotFound)
            {
                return true;
            }

            current = current.InnerException;
        }

        return false;
    }
}