using Microsoft.AspNetCore.DataProtection;

namespace TheAdamsParadigm.Api.Services;

// Encrypts/decrypts sensitive per-client credentials (currently Client.ICloudPassword)
// before they're written to or read from the database, so the database never holds
// a plaintext password. Uses ASP.NET Core's Data Protection API — ships with the
// framework, no extra package or key management needed.
public class ClientCredentialProtector
{
    private readonly IDataProtector _protector;

    public ClientCredentialProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("TheAdamsParadigm.Client.ICloudPassword.v1");
    }

    public string Protect(string plaintext) => _protector.Protect(plaintext);

    public string Unprotect(string protectedText) => _protector.Unprotect(protectedText);
}
