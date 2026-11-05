using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Kaban.Api.Features.Auth.Application.DTO;
using Kaban.Api.Features.Auth.Infrastructure;
using Kaban.Api.Features.Users.Application.DTO;
using Kaban.Contracts.Users;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kaban.Tests;

public sealed class AuthEndpointTests : IClassFixture<AuthApiFactory>
{
    private readonly AuthApiFactory factory;
    private readonly HttpClient client;
    public AuthEndpointTests(AuthApiFactory factory)
    {
        this.factory = factory;
        client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_Me_Search_Refresh_UseDistinctTokenPolicies()
    {
        var login = await client.PostAsJsonAsync("/api/auth/sessions", new { email = " USER@example.com ", password = "good-password" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var tokens = (await login.Content.ReadFromJsonAsync<TokenPairDto>())!;
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/users/me")).StatusCode);

        client.DefaultRequestHeaders.Authorization = new("Bearer", tokens.AccessToken);
        var me = await client.GetFromJsonAsync<UserDto>("/api/users/me");
        Assert.Equal(7, me!.Id);
        var search = await client.GetFromJsonAsync<UserDto[]>("/api/users?query=user&limit=10");
        Assert.Single(search!);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsync("/api/auth/tokens/refresh", null)).StatusCode);

        client.DefaultRequestHeaders.Authorization = new("Bearer", tokens.RefreshToken);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/users/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/users?query=user")).StatusCode);
        var refreshed = await client.PostAsync("/api/auth/tokens/refresh", null);
        Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
        Assert.NotEqual(tokens.RefreshToken, (await refreshed.Content.ReadFromJsonAsync<TokenPairDto>())!.RefreshToken);
    }

    [Fact]
    public async Task InvalidInput_IsRejectedBeforeMessaging()
    {
        var before = factory.Services.GetRequiredService<Probe>().Calls;
        var badLogin = await client.PostAsJsonAsync("/api/auth/sessions", new { email = "bad", password = "" });
        Assert.Equal(HttpStatusCode.BadRequest, badLogin.StatusCode);
        var badRegistration = await client.PostAsJsonAsync("/api/users", new { email = "ok@example.com", name = "Name", password = "good-password" });
        Assert.Equal(HttpStatusCode.BadRequest, badRegistration.StatusCode);
        var tokens = factory.Services.GetRequiredService<TokenService>().Create(7);
        client.DefaultRequestHeaders.Authorization = new("Bearer", tokens.AccessToken);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/users?query=user&limit=101")).StatusCode);
        Assert.Equal(before, factory.Services.GetRequiredService<Probe>().Calls);
    }

    [Theory]
    [InlineData("missing@example.com", "good-password")]
    [InlineData("user@example.com", "wrong-password")]
    public async Task Login_UsesSameFailureForUnknownEmailAndBadPassword(string email, string password)
    {
        var result = await client.PostAsJsonAsync("/api/auth/sessions", new { email, password });
        Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
        Assert.Contains("Invalid credentials", await result.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Register_WaitsForWorker_AndDoesNotExposePasswordHash()
    {
        var probe = factory.Services.GetRequiredService<Probe>();
        probe.ReleaseRegistration = new(TaskCreationOptions.RunContinuationsAsynchronously);
        probe.RegistrationReceived = new(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/users")
            {
                Content = JsonContent.Create(new { email = " NEW@example.com ", name = " New ", password = "good-password" })
            };
            request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
            var pending = client.SendAsync(request);
            await probe.RegistrationReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.False(pending.IsCompleted);
            probe.ReleaseRegistration.SetResult();
            var response = await pending;
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Equal("", await response.Content.ReadAsStringAsync());
        }
        finally { probe.ReleaseRegistration.TrySetResult(); }
    }

    [Theory]
    [InlineData(88, HttpStatusCode.NotFound)]
    [InlineData(99, HttpStatusCode.BadGateway)]
    [InlineData(100, HttpStatusCode.ServiceUnavailable)]
    public async Task WorkerErrors_AreDistinctAndDoNotLeakDetails(int userId, HttpStatusCode expected)
    {
        client.DefaultRequestHeaders.Authorization = new("Bearer", factory.Services.GetRequiredService<TokenService>().Create(userId).AccessToken);
        var response = await client.GetAsync("/api/users/me");
        Assert.Equal(expected, response.StatusCode);
        Assert.DoesNotContain("private-database-detail", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task InvalidSignature_IsRejected()
    {
        var other = new TokenService(new AuthOptions { SigningKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)) });
        client.DefaultRequestHeaders.Authorization = new("Bearer", other.Create(7).AccessToken);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/users/me")).StatusCode);
    }
}

public sealed class AuthApiFactory : WebApplicationFactory<Kaban.Api.Features.Auth.Presentation.AuthController>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Auth:SigningKey", Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)));
        builder.UseSetting("Messaging:RequestTimeoutSeconds", "3");
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<Probe>();
            services.AddMassTransitTestHarness(x =>
            {
                x.AddConsumer<StubUsersConsumer>();
                x.AddRequestClient<GetCredentialsRequested>(RequestTimeout.After(s: 3));
                x.AddRequestClient<GetUserRequested>(RequestTimeout.After(s: 3));
                x.AddRequestClient<SearchUsersRequested>(RequestTimeout.After(s: 3));
                x.AddRequestClient<RegisterUserRequested>(RequestTimeout.After(s: 3));
                x.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));
            });
        });
    }
}

public sealed class Probe
{
    public int Calls;
    public TaskCompletionSource? ReleaseRegistration;
    public TaskCompletionSource? RegistrationReceived;
}

public sealed class StubUsersConsumer(CredentialsService credentials, Probe probe) :
    IConsumer<GetCredentialsRequested>, IConsumer<GetUserRequested>,
    IConsumer<SearchUsersRequested>, IConsumer<RegisterUserRequested>
{
    public async Task Consume(ConsumeContext<GetCredentialsRequested> context)
    {
        Interlocked.Increment(ref probe.Calls);
        if (context.Message.Email == "user@example.com")
            await context.RespondAsync(new CredentialsFound(7, credentials.Hash("good-password")));
        else await context.RespondAsync(new UserFailure("invalid_credentials"));
    }

    public async Task Consume(ConsumeContext<GetUserRequested> context)
    {
        Interlocked.Increment(ref probe.Calls);
        switch (context.Message.UserId)
        {
            case 88: await context.RespondAsync(new UserFailure("not_found")); return;
            case 99: throw new InvalidOperationException("private-database-detail");
            case 100: return;
            default: await context.RespondAsync(new UserResult(7, "User", "user@example.com")); return;
        }
    }

    public async Task Consume(ConsumeContext<SearchUsersRequested> context)
    {
        Interlocked.Increment(ref probe.Calls);
        await context.RespondAsync(new UsersFound([new UserResult(7, "User", "user@example.com")]));
    }

    public async Task Consume(ConsumeContext<RegisterUserRequested> context)
    {
        Interlocked.Increment(ref probe.Calls);
        Assert.Equal("new@example.com", context.Message.Email);
        Assert.Equal("New", context.Message.Name);
        Assert.NotEqual("good-password", context.Message.PasswordHash);
        Assert.True(credentials.Verify("good-password", context.Message.PasswordHash));
        probe.RegistrationReceived?.TrySetResult();
        if (probe.ReleaseRegistration is not null) await probe.ReleaseRegistration.Task;
        await context.RespondAsync(new UserRegistered(7));
    }
}

