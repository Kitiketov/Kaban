using Contracts.Features.Users.Common;
using Contracts.Features.Users.GetCredentials;
using Kaban.Api.Common.Exceptions;
using Kaban.Api.Features.Auth.Application.DTO;
using Kaban.Api.Features.Auth.Infrastructure;
using MassTransit;
using MediatR;

namespace Kaban.Api.Features.Auth.Application.Login;

public sealed class LoginHandler(
    IRequestClient<GetCredentialsRequest> client,
    CredentialsService credentials,
    TokenService tokens) : IRequestHandler<LoginCommand, TokenPairDto>
{
    public async Task<TokenPairDto> Handle(LoginCommand request, CancellationToken ct)
    {
        var response = await client.GetResponse<GetCredentialsSuccess, UserFailure>(
            new(request.Email.Trim().ToLowerInvariant()), ct);
        if (response.Is(out Response<UserFailure>? _))
        {
            credentials.Verify(request.Password, null);
            throw new UserOperationException("invalid_credentials");
        }

        var user = (response.Is(out Response<GetCredentialsSuccess>? success)
            ? success.Message
            : throw new InvalidOperationException("Unexpected Worker response"));
        if (!credentials.Verify(request.Password, user.PasswordHash))
            throw new UserOperationException("invalid_credentials");
        return tokens.Create(user.UserId);
    }
}
