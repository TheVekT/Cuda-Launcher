using System.Security.Cryptography;
using System.Text;

namespace Launcher.Core.Common.Helpers;

public static class SecurityHelper
{
    public static string? Protect(string? plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return plainText;

        try
        {
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var cipherBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(cipherBytes);
        }
        catch
        {
            return null;
        }
    }
    
    public static string? Unprotect(string? cipherText)
    {
        if (string.IsNullOrEmpty(cipherText)) return cipherText;

        try
        {
            var cipherBytes = Convert.FromBase64String(cipherText);
            var plainBytes = ProtectedData.Unprotect(cipherBytes, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plainBytes);
        }
        catch
        {

            return null; 
        }
    }
}
