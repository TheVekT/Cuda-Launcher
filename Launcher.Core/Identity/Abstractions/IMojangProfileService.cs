using Launcher.Core.Common.Enums;
using Launcher.Core.Identity.Models;

namespace Launcher.Core.Identity.Abstractions;

public interface IMojangProfileService
{
    Task<MojangProfile?> GetProfileAsync(string accessToken);

    Task<NetworkRequestStatus> UploadSkinAsync(string accessToken, string filePath, string variant = "classic");
    
    Task<NetworkRequestStatus> ApplyCapeAsync(string accessToken, string capeId);
    
    Task<NetworkRequestStatus> HideCapeAsync(string accessToken);
}