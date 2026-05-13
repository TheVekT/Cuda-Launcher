using Launcher.Core.Identity.Models;

namespace Launcher.Core.Identity.Abstractions;

public interface IMojangProfileService
{
    Task<MojangProfile?> GetProfileAsync(string accessToken);

    Task<bool> UploadSkinAsync(string accessToken, string filePath, string variant = "classic");
    
    Task<bool> ApplyCapeAsync(string accessToken, string capeId);
    
    Task<bool> HideCapeAsync(string accessToken);
}