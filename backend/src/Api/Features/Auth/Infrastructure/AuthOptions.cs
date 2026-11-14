namespace Kaban.Api.Features.Auth.Infrastructure;

public sealed class AuthOptions
{
    public string SigningKey { get; set; } = "";
    public string Issuer { get; set; } = "kaban";
    public string Audience { get; set; } = "kaban-client";
    public int AccessMinutes { get; set; } = 15;
    public int RefreshDays { get; set; } = 7;
}