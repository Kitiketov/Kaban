using Contracts.Features.Users.Common;
using Contracts.Features.Users.Get;
using Kaban.Api.Common.Exceptions;
using Kaban.Api.Features.Users.Application.DTO;
using MassTransit;
using MediatR;

namespace Kaban.Api.Features.Users.Application.GetMe;

public sealed class GetMeHandler(IRequestClient<GetUserRequest> client) : IRequestHandler<GetMeQuery, UserDto>
{
    public async Task<UserDto> Handle(GetMeQuery request, CancellationToken ct)
    {
        var response = await client.GetResponse<UserSuccess, UserFailure>(new(request.UserId), ct);
        if (response.Is(out Response<UserFailure>? failure)) throw new UserOperationException(failure.Message.Code);
        var user = (response.Is(out Response<UserSuccess>? success)
            ? success.Message
            : throw new InvalidOperationException("Unexpected Worker response"));
        return new(user.Id, user.Name, user.Email);
    }
}