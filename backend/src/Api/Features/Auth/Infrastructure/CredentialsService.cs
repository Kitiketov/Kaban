using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;

namespace Kaban.Api.Features.Auth.Infrastructure;

public sealed class CredentialsService(PasswordHasher<object> hasher, AuthOptions options)
{
    private readonly string dummyHash = hasher.HashPassword(new object(), Guid.NewGuid().ToString());
    public string Hash(string password) => hasher.HashPassword(new object(), password);

    public bool Verify(string password, string? hash) =>
        hasher.VerifyHashedPassword(new object(), hash ?? dummyHash, password) != PasswordVerificationResult.Failed
        && hash is not null;

    // Stable across HTTP retries despite random password salts. Keyed to avoid a cheap password oracle.
    public string Fingerprint(string email, string name, string password) =>
        Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(options.SigningKey),
            JsonSerializer.SerializeToUtf8Bytes(new[] { "register-user-v1", email, name, password })));
}