using System.Security.Cryptography;

namespace DeepSeekCreditCheck.Core.Configuration;

public static class DataProtection
{
    public static string Protect(string plainText)
    {
        byte[] plainBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
        byte[] protectedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(protectedBytes);
    }

    public static string Unprotect(string protectedText)
    {
        byte[] protectedBytes = Convert.FromBase64String(protectedText);
        byte[] plainBytes = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
        return System.Text.Encoding.UTF8.GetString(plainBytes);
    }
}
