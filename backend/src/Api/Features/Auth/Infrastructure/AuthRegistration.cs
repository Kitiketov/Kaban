using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace Kaban.Api.Features.Auth.Infrastructure;

public static class AuthRegistration
{
    public const string RefreshPolicy = "RefreshTokenOnly";

    public static IServiceCollection AddKabanAuth(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection("Auth").Get<AuthOptions>() ?? new();
        if (Encoding.UTF8.GetByteCount(settings.SigningKey) < 32 ||
            string.IsNullOrWhiteSpace(settings.Issuer) || string.IsNullOrWhiteSpace(settings.Audience) ||
            settings.AccessMinutes is < 1 or > 60 || settings.RefreshDays is < 1 or > 30)
            throw new InvalidOperationException(
                "Configure Auth:SigningKey (at least 32 bytes), issuer, audience and finite token lifetimes.");
        services.AddSingleton(settings);
        services.AddSingleton<TokenService>();
        services.AddSingleton<PasswordHasher<object>>(_ => new PasswordHasher<object>(
            Microsoft.Extensions.Options.Options.Create(new PasswordHasherOptions { IterationCount = 210000 })));
        services.AddSingleton<CredentialsService>();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.MapInboundClaims = false;
            options.TokenValidationParameters = TokenService.ValidationParameters(settings);
        });
        services.AddAuthorization(options =>
        {
            options.DefaultPolicy = Policy("access");
            options.AddPolicy(RefreshPolicy, Policy("refresh"));
        });
        return services;
    }

    private static AuthorizationPolicy Policy(string type) =>
        new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
            .RequireAuthenticatedUser()
            .RequireClaim("token_type", type)
            .RequireAssertion(c => int.TryParse(c.User.FindFirst("user_id")?.Value,
                NumberStyles.None, CultureInfo.InvariantCulture, out var id) && id > 0)
            .Build();
}