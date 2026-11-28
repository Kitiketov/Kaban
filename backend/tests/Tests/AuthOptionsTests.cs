using Kaban.Api.Features.Auth.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kaban.Tests;

public sealed class AuthOptionsTests
{
    [Theory]
    [InlineData("SigningKey", null)]
    [InlineData("SigningKey", "short-secret")]
    [InlineData("SigningKey", "                                ")]
    [InlineData("Issuer", null)]
    [InlineData("Audience", " ")]
    [InlineData("AccessMinutes", "0")]
    [InlineData("AccessMinutes", "61")]
    [InlineData("RefreshDays", "0")]
    [InlineData("RefreshDays", "31")]
    public void Registration_rejects_invalid_configuration_without_exposing_key(string property, string? value)
    {
        var values = ValidConfiguration();
        values["Auth:" + property] = value;
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();

        var error = Assert.ThrowsAny<ArgumentException>(() => new ServiceCollection().AddKabanAuth(configuration));

        Assert.Contains("Auth:" + property, error.Message);
        Assert.DoesNotContain(values["Auth:SigningKey"] ?? "unused-secret", error.Message);
    }

    [Fact]
    public void Registration_uses_optional_lifetimes_and_preserves_configured_identity()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(ValidConfiguration()).Build();
        using var provider = new ServiceCollection().AddKabanAuth(configuration).BuildServiceProvider();
        var options = provider.GetRequiredService<AuthOptions>();

        Assert.Equal("test-issuer", options.Issuer);
        Assert.Equal("test-audience", options.Audience);
        Assert.Equal(15, options.AccessMinutes);
        Assert.Equal(7, options.RefreshDays);
    }

    private static Dictionary<string, string?> ValidConfiguration() => new()
    {
        ["Auth:SigningKey"] = "test-only-signing-key-at-least-32-bytes",
        ["Auth:Issuer"] = "test-issuer",
        ["Auth:Audience"] = "test-audience"
    };
}
