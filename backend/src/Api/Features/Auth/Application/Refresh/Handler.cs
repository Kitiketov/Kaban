using Contracts.Features.Users;
using Kaban.Api.Common.Exceptions;
using Kaban.Api.Features.Auth.Application.DTO;
using Kaban.Api.Features.Auth.Infrastructure;
using MassTransit;
using MediatR;

namespace Kaban.Api.Features.Auth.Application.Refresh;

public sealed class RefreshHandler(IRequestClient<GetUserRequested> client, TokenService tokens)
    : IRequestHandler<RefreshCommand, TokenPairDto>
{
    public async Task<TokenPairDto> Handle(RefreshCommand request, CancellationToken ct)
    {
        var response = await client.GetResponse<UserResult, UserFailure>(new(request.UserId), ct);
        if (response.Is(out Response<UserFailure>? _)) throw new UserOperationException("unauthorized");
        return tokens.Create((response.Is(out Response<UserResult>? success)
            ? success.Message
            : throw new InvalidOperationException("Unexpected Worker response")).Id);
    }
}