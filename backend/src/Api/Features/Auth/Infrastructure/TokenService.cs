using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Kaban.Api.Features.Auth.Application.DTO;
using Microsoft.IdentityModel.Tokens;

namespace Kaban.Api.Features.Auth.Infrastructure;

public sealed class TokenService(AuthOptions settings)
{
    public TokenPairDto Create(int userId) => new(
        CreateToken(userId, "access", TimeSpan.FromMinutes(settings.AccessMinutes)),
        CreateToken(userId, "refresh", TimeSpan.FromDays(settings.RefreshDays)));

    private string CreateToken(int userId, string type, TimeSpan lifetime)
    {
        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(settings.Issuer, settings.Audience,
            [
                new Claim("user_id", userId.ToString(CultureInfo.InvariantCulture)),
                new Claim("token_type", type), new Claim("jti", Guid.NewGuid().ToString("N"))
            ],
            notBefore: now, expires: now.Add(lifetime),
            signingCredentials: new SigningCredentials(Key(settings), SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static SymmetricSecurityKey Key(AuthOptions options) => new(Encoding.UTF8.GetBytes(options.SigningKey));

    public static TokenValidationParameters ValidationParameters(AuthOptions options) => new()
    {
        ValidateIssuer = true, ValidIssuer = options.Issuer,
        ValidateAudience = true, ValidAudience = options.Audience,
        ValidateIssuerSigningKey = true, IssuerSigningKey = Key(options),
        ValidateLifetime = true, RequireExpirationTime = true, RequireSignedTokens = true,
        ValidAlgorithms = [SecurityAlgorithms.HmacSha256], ClockSkew = TimeSpan.Zero,
        NameClaimType = "user_id"
    };
}