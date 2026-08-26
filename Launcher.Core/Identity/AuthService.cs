using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;
using CmlLib.Core.Auth;
using CmlLib.Core.Auth.Microsoft;
using Launcher.Core.Config.Abstractions;
using Launcher.Core.Identity.Abstractions;
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
            if (authenticator.Context?.SessionStorage != null)
            {
                authenticator.Context.SessionStorage.Set<string>("Identifier", session.UUID);
            }

            // Persist session data to cml_accounts.json
            _loginHandler.AccountManager.SaveAccounts();

            var accounts = _loginHandler.AccountManager.GetAccounts();
            Debug.WriteLine($"[Auth] Interactive login completed. Records in cml_accounts.json: {accounts.Count}");

            return new UserAccount(
                session.Username, 
                session.UUID, 
                session.AccessToken, 
                isOffline: false);
        }
        catch (Exception ex)
        {
            throw new Exception($"Microsoft Login failed: {ex.Message}");
        }
    }

    public UserAccount LoginOffline(string nickname)
    {
        var offlineSession = MSession.CreateOfflineSession(nickname);
        return new UserAccount(offlineSession.Username, offlineSession.UUID, null, isOffline: true);
    }

    public async Task<UserAccount> ValidateAndRefreshAccountAsync(UserAccount account)
    {
        if (account == null) return null;
        if (account.IsOffline) return account;
        
        if (string.IsNullOrEmpty(account.AccessToken))
            throw new UnauthorizedAccessException("Token is missing.");
        
        try
        {
            Debug.WriteLine($"[Auth] Validating token for {account.Username}...");
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.minecraftservices.com/minecraft/profile");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", account.AccessToken);
            
            var response = await _httpClient.SendAsync(request);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                if (doc.RootElement.TryGetProperty("name", out var nameElement))
                {
                    account.Username = nameElement.GetString();
                }
                Debug.WriteLine($"[Auth] Token is valid. User: {account.Username}");
                return account;
            }
            else
            {
                Debug.WriteLine("[Auth] Token expired! Starting built-in Silent Refresh...");
                
                var authenticator = _loginHandler.CreateAuthenticatorWithDefaultAccount();

                // Silent refresh mode
                authenticator.AddMicrosoftOAuthForJE(oauth => oauth.Silent()); 
                authenticator.AddXboxAuthForJE(xbox => xbox.Basic());
                authenticator.AddJEAuthenticator();

                var newSession = await authenticator.ExecuteForLauncherAsync();

                // Force persist the refreshed tokens to the disk
                _loginHandler.AccountManager.SaveAccounts();

                account.AccessToken = newSession.AccessToken;
                account.Username = newSession.Username;
                account.UUID = newSession.UUID;

                Debug.WriteLine("[Auth] Silent Refresh pipeline completed successfully!");
                return account;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Auth Error] Refresh failed: {ex.Message}");
            throw new Exception("Session expired or cache is empty. Manual Microsoft login required.", ex);
        }
    }
}