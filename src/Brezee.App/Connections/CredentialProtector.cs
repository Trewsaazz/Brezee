using System.Security.Cryptography;
using System.Text;

namespace Brezee.App.Connections;

// Encrypts saved passwords so they are never stored as plain text.
public interface ICredentialProtector
{
    // Returns an encrypted, printable form of the password.
    string Protect(string password);

    // Returns the password, or null if it cannot be decrypted here (another Windows user or PC,
    // or damaged data).
    string? Unprotect(string protectedPassword);
}

// Windows DPAPI, scoped to the current user: only the same Windows account on the same machine
// can decrypt, and Brezee never has to manage a key.
public sealed class DpapiCredentialProtector : ICredentialProtector
{
    // Ties the encrypted data to Brezee, so other DPAPI data of the user cannot be swapped in.
    private static readonly byte[] Entropy = "Brezee.SavedConnection.Password.v1"u8.ToArray();

    public string Protect(string password)
    {
        var encrypted = ProtectedData.Protect(Encoding.UTF8.GetBytes(password), Entropy, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(encrypted);
    }

    public string? Unprotect(string protectedPassword)
    {
        try
        {
            var decrypted = ProtectedData.Unprotect(Convert.FromBase64String(protectedPassword), Entropy, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decrypted);
        }
        catch (Exception ex) when (ex is CryptographicException or FormatException)
        {
            return null;
        }
    }
}
