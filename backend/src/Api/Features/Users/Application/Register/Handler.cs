using Contracts.Features.Users.Common;
using Contracts.Features.Users.Register;
using Kaban.Api.Common.Exceptions;
using Kaban.Api.Features.Auth.Infrastructure;
using MassTransit;
using MediatR;
namespace Kaban.Api.Features.Users.Application.Register;

public sealed class RegisterUserHandler(IRequestClient<RegisterUserRequest> client, CredentialsService credentials)
    : IRequestHandler<RegisterUserCommand>
{
    public async Task Handle(RegisterUserCommand request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var name = request.Name.Trim();
        var response = await client.GetResponse<RegisterUserSuccess, UserFailure>(
            new(email, name, credentials.Hash(request.Password)), ct);
        if (response.Is(out Response<UserFailure>? failure)) throw new UserOperationException(failure.Message.Code);
    }
}
