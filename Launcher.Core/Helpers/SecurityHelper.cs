using System;
using System.Security.Cryptography;
using System.Text;

namespace Launcher.Core.Helpers;

public static class SecurityHelper
{
    public static string Protect(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return plainText;

        try
        {
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] cipherBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(cipherBytes);
        }
        catch
        {
            return null;
        }
    }

    // Расшифровываем строку
    public static string Unprotect(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText)) return cipherText;

        try
        {
            byte[] cipherBytes = Convert.FromBase64String(cipherText);
            byte[] plainBytes = ProtectedData.Unprotect(cipherBytes, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plainBytes);
        }
        catch
        {

            return null; 
        }
    }
}
