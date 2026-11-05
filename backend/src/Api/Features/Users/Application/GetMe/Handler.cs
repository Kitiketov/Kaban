using Kaban.Api.Common.Exceptions;
using Kaban.Api.Features.Users.Application.DTO;
using Kaban.Contracts.Users;
using MassTransit;
using MediatR;

namespace Kaban.Api.Features.Users.Application.GetMe;

public sealed class GetMeHandler(IRequestClient<GetUserRequested> client) : IRequestHandler<GetMeQuery, UserDto>
{
    public async Task<UserDto> Handle(GetMeQuery request, CancellationToken ct)
    {
        var response = await client.GetResponse<UserResult, UserFailure>(new(request.UserId), ct);
        if (response.Is(out Response<UserFailure>? failure)) throw new UserOperationException(failure.Message.Code);
        var user = (response.Is(out Response<UserResult>? success)
            ? success.Message
            : throw new InvalidOperationException("Unexpected Worker response"));
        return new(user.Id, user.Name, user.Email);
    }
}