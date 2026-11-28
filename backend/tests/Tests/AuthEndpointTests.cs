using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Contracts.Features.Users.Common;
using Contracts.Features.Users.Get;
using Contracts.Features.Users.GetCredentials;
using Contracts.Features.Users.Register;
using Contracts.Features.Users.Search;
using Kaban.Api.Features.Auth.Application.DTO;
using Kaban.Api.Features.Auth.Infrastructure;
using Kaban.Api.Features.Users.Application.DTO;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Xunit;

namespace Kaban.Tests;

public sealed class AuthEndpointTests : IDisposable
{
    private readonly AuthApiFactory factory;
    private readonly HttpClient client;
    public AuthEndpointTests()
    {
        factory = new AuthApiFactory();
        client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_Me_Search_Refresh_UseDistinctTokenPolicies()
    {
        factory.Credentials.Setup(x => x.GetResponse<GetCredentialsSuccess, UserFailure>(
            new GetCredentialsRequest("user@example.com"), It.IsAny<CancellationToken>(), It.IsAny<RequestTimeout>()))
            .ReturnsAsync(AuthApiFactory.Success<GetCredentialsSuccess>(new(AuthTestUsers.ExistingUserId,
                factory.Services.GetRequiredService<CredentialsService>().Hash("good-password"))));
        factory.Users.Setup(x => x.GetResponse<UserSuccess, UserFailure>(
            new GetUserRequest(AuthTestUsers.ExistingUserId), It.IsAny<CancellationToken>(), It.IsAny<RequestTimeout>()))
            .ReturnsAsync(AuthApiFactory.Success<UserSuccess>(new(AuthTestUsers.ExistingUserId, "User", "user@example.com")));
        factory.Search.Setup(x => x.GetResponse<SearchUsersSuccess, UserFailure>(
            new SearchUsersRequest(AuthTestUsers.ExistingUserId, "user", 10), It.IsAny<CancellationToken>(), It.IsAny<RequestTimeout>()))
            .ReturnsAsync(AuthApiFactory.Success<SearchUsersSuccess>(new([new(AuthTestUsers.ExistingUserId, "User", "user@example.com")])));
        var login = await client.PostAsJsonAsync("/api/auth/sessions", new { email = " USER@example.com ", password = "good-password" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var tokens = (await login.Content.ReadFromJsonAsync<TokenPairDto>())!;
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/users/me")).StatusCode);

        client.DefaultRequestHeaders.Authorization = new("Bearer", tokens.AccessToken);
        var me = await client.GetFromJsonAsync<UserDto>("/api/users/me");
        Assert.Equal(AuthTestUsers.ExistingUserId, me!.Id);
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
        var badLogin = await client.PostAsJsonAsync("/api/auth/sessions", new { email = "bad", password = "" });
        Assert.Equal(HttpStatusCode.BadRequest, badLogin.StatusCode);
        var badRegistration = await client.PostAsJsonAsync("/api/users", new { email = "ok@example.com", name = "Name", password = "short" });
        Assert.Equal(HttpStatusCode.BadRequest, badRegistration.StatusCode);
        var tokens = factory.Services.GetRequiredService<TokenService>().Create(AuthTestUsers.ExistingUserId);
        client.DefaultRequestHeaders.Authorization = new("Bearer", tokens.AccessToken);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/users?query=user&limit=101")).StatusCode);
        factory.Credentials.VerifyNoOtherCalls();
        factory.Registration.VerifyNoOtherCalls();
        factory.Search.VerifyNoOtherCalls();
        factory.Users.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("missing@example.com", "good-password")]
    [InlineData("user@example.com", "wrong-password")]
    public async Task Login_UsesSameFailureForUnknownEmailAndBadPassword(string email, string password)
    {
        var setup = factory.Credentials.Setup(x => x.GetResponse<GetCredentialsSuccess, UserFailure>(
            new GetCredentialsRequest(email), It.IsAny<CancellationToken>(), It.IsAny<RequestTimeout>()));
        if (email == "missing@example.com")
            setup.ReturnsAsync(AuthApiFactory.Failure<GetCredentialsSuccess>("invalid_credentials"));
        else
            setup.ReturnsAsync(AuthApiFactory.Success<GetCredentialsSuccess>(new(AuthTestUsers.ExistingUserId,
                factory.Services.GetRequiredService<CredentialsService>().Hash("good-password"))));
        var result = await client.PostAsJsonAsync("/api/auth/sessions", new { email, password });
        Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
        Assert.Contains("Invalid credentials", await result.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Register_WithoutIdempotencyKey_WaitsForWorker_AndDoesNotExposePasswordHash()
    {
        var received = new TaskCompletionSource<RegisterUserRequest>(TaskCreationOptions.RunContinuationsAsynchronously);
        var reply = new TaskCompletionSource<Response<RegisterUserSuccess, UserFailure>>(TaskCreationOptions.RunContinuationsAsynchronously);
        factory.Registration.Setup(x => x.GetResponse<RegisterUserSuccess, UserFailure>(
            It.IsAny<RegisterUserRequest>(), It.IsAny<CancellationToken>(), It.IsAny<RequestTimeout>()))
            .Callback<RegisterUserRequest, CancellationToken, RequestTimeout>((message, _, _) => received.TrySetResult(message))
            .Returns(reply.Task);
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/users")
            {
                Content = JsonContent.Create(new { email = " NEW@example.com ", name = " New ", password = "good-password" })
            };
            var pending = client.SendAsync(request);
            var message = await received.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.Equal("new@example.com", message.Email);
            Assert.Equal("New", message.Name);
            Assert.NotEqual("good-password", message.PasswordHash);
            Assert.True(factory.Services.GetRequiredService<CredentialsService>().Verify("good-password", message.PasswordHash));
            Assert.False(pending.IsCompleted);
            reply.SetResult(AuthApiFactory.Success<RegisterUserSuccess>(new(AuthTestUsers.ExistingUserId)));
            var response = await pending;
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Equal("", await response.Content.ReadAsStringAsync());
        }
        finally { reply.TrySetCanceled(); }
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task WorkerErrors_AreDistinctAndDoNotLeakDetails(HttpStatusCode expected)
    {
        var setup = factory.Users.Setup(x => x.GetResponse<UserSuccess, UserFailure>(
            new GetUserRequest(AuthTestUsers.ExistingUserId), It.IsAny<CancellationToken>(), It.IsAny<RequestTimeout>()));
        if (expected == HttpStatusCode.NotFound)
            setup.ReturnsAsync(AuthApiFactory.Failure<UserSuccess>("not_found"));
        else if (expected == HttpStatusCode.BadGateway)
            setup.ThrowsAsync(new RequestFaultException("private-database-detail", Mock.Of<Fault>(x => x.Exceptions == Array.Empty<ExceptionInfo>())));
        else
            setup.ThrowsAsync(new RequestTimeoutException(Guid.NewGuid().ToString()));
        client.DefaultRequestHeaders.Authorization = new("Bearer", factory.Services.GetRequiredService<TokenService>().Create(AuthTestUsers.ExistingUserId).AccessToken);
        var response = await client.GetAsync("/api/users/me");
        Assert.Equal(expected, response.StatusCode);
        Assert.DoesNotContain("private-database-detail", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task InvalidSignature_IsRejected()
    {
        var other = new TokenService(new AuthOptions(Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)), "kaban", "kaban-client"));
        client.DefaultRequestHeaders.Authorization = new("Bearer", other.Create(AuthTestUsers.ExistingUserId).AccessToken);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/users/me")).StatusCode);
    }
    public void Dispose()
    {
        client.Dispose();
        factory.Dispose();
    }
}

internal static class AuthTestUsers
{
    public const int ExistingUserId = 7;

}

public sealed class Probe
{
    public int Calls;
    public TaskCompletionSource? ReleaseRegistration;
    public TaskCompletionSource? RegistrationReceived;
}

public sealed class AuthApiFactory : WebApplicationFactory<Kaban.Api.Features.Auth.Presentation.AuthController>
{
    public Mock<IRequestClient<GetCredentialsRequest>> Credentials { get; } = new(MockBehavior.Strict);
    public Mock<IRequestClient<GetUserRequest>> Users { get; } = new(MockBehavior.Strict);
    public Mock<IRequestClient<SearchUsersRequest>> Search { get; } = new(MockBehavior.Strict);
    public Mock<IRequestClient<RegisterUserRequest>> Registration { get; } = new(MockBehavior.Strict);

    public static Response<T, UserFailure> Success<T>(T message) where T : class =>
        new(Task.FromResult(Mock.Of<Response<T>>(x => x.Message == message)),
            new TaskCompletionSource<Response<UserFailure>>().Task);

    public static Response<T, UserFailure> Failure<T>(string code) where T : class =>
        new(new TaskCompletionSource<Response<T>>().Task,
            Task.FromResult(Mock.Of<Response<UserFailure>>(x => x.Message == new UserFailure(code))));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Auth:SigningKey", Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)));
        builder.ConfigureServices(services =>
        {
            // HTTP tests replace messaging clients and do not run background infrastructure.
            services.RemoveAll<Microsoft.Extensions.Hosting.IHostedService>();
            services.AddSingleton(Credentials.Object);
            services.AddSingleton(Users.Object);
            services.AddSingleton(Search.Object);
            services.AddSingleton(Registration.Object);
        });
    }
}
