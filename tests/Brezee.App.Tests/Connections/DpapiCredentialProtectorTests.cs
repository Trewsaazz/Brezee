using Brezee.App.Connections;

namespace Brezee.App.Tests.Connections;

// Uses the real Windows DPAPI.
public class DpapiCredentialProtectorTests
{
    private readonly DpapiCredentialProtector _protector = new();

    [Theory]
    [InlineData("masterkey")]
    [InlineData("pässwörd with ünïcödé 🔑")]
    [InlineData("")]
    public void ProtectThenUnprotect_RoundTrips(string password)
    {
        Assert.Equal(password, _protector.Unprotect(_protector.Protect(password)));
    }

    [Fact]
    public void Protect_NeverContainsThePlainPassword()
    {
        var protectedPassword = _protector.Protect("masterkey");

        Assert.DoesNotContain("masterkey", protectedPassword);
        Assert.DoesNotContain(Convert.ToBase64String("masterkey"u8.ToArray()), protectedPassword);
    }

    [Theory]
    [InlineData("not base64 at all!")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAA==")]
    public void Unprotect_DataItCannotDecrypt_ReturnsNull(string protectedPassword)
    {
        Assert.Null(_protector.Unprotect(protectedPassword));
    }
}
