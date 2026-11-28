using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace Kaban.Api.Features.Auth.Infrastructure;

public static class AuthRegistration
{
    public const string RefreshPolicy = "RefreshTokenOnly";

    public static IServiceCollection AddKabanAuth(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection("Auth");
        var settings = new AuthOptions(
            section["SigningKey"], section["Issuer"], section["Audience"],
            section.GetValue<int?>("AccessMinutes") ?? 15,
            section.GetValue<int?>("RefreshDays") ?? 7);
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
        services.AddRateLimiter(options =>
        {
            options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 20,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));
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
