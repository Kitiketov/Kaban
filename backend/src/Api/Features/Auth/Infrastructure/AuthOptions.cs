using System.Text;

namespace Kaban.Api.Features.Auth.Infrastructure;

public sealed class AuthOptions
{
    public AuthOptions(string? signingKey, string? issuer, string? audience,
        int accessMinutes = 15, int refreshDays = 7)
    {
        if (string.IsNullOrWhiteSpace(signingKey))
            throw new ArgumentException("Auth:SigningKey is required.", nameof(signingKey));
        if (Encoding.UTF8.GetByteCount(signingKey) < 32)
            throw new ArgumentException("Auth:SigningKey must contain at least 32 UTF-8 bytes.", nameof(signingKey));
        if (string.IsNullOrWhiteSpace(issuer))
            throw new ArgumentException("Auth:Issuer is required.", nameof(issuer));
        if (string.IsNullOrWhiteSpace(audience))
            throw new ArgumentException("Auth:Audience is required.", nameof(audience));
        if (accessMinutes is < 1 or > 60)
            throw new ArgumentOutOfRangeException(nameof(accessMinutes), "Auth:AccessMinutes must be between 1 and 60.");
        if (refreshDays is < 1 or > 30)
            throw new ArgumentOutOfRangeException(nameof(refreshDays), "Auth:RefreshDays must be between 1 and 30.");

        SigningKey = signingKey;
        Issuer = issuer;
        Audience = audience;
        AccessMinutes = accessMinutes;
        RefreshDays = refreshDays;
    }

    public string SigningKey { get; }
    public string Issuer { get; }
    public string Audience { get; }
    public int AccessMinutes { get; }
    public int RefreshDays { get; }
}
