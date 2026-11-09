using Microsoft.AspNetCore.Identity;

namespace Kaban.Api.Features.Auth.Infrastructure;

public sealed class CredentialsService(PasswordHasher<object> hasher)
{
    private readonly string dummyHash = hasher.HashPassword(new object(), Guid.NewGuid().ToString());
    public string Hash(string password) => hasher.HashPassword(new object(), password);

    public bool Verify(string password, string? hash) =>
        hasher.VerifyHashedPassword(new object(), hash ?? dummyHash, password) != PasswordVerificationResult.Failed
        && hash is not null;

}
