using Contracts.Features.Users;
using Kaban.Api.Common.Exceptions;
using Kaban.Api.Features.Users.Application.DTO;
using MassTransit;
using MediatR;

namespace Kaban.Api.Features.Users.Application.Search;

public sealed class SearchUsersHandler(IRequestClient<SearchUsersRequested> client)
    : IRequestHandler<SearchUsersQuery, UserDto[]>
{
    public async Task<UserDto[]> Handle(SearchUsersQuery request, CancellationToken ct)
    {
        var response = await client.GetResponse<UsersFound, UserFailure>(
            new(request.ActorId, request.Query.Trim(), request.Limit), ct);
        if (response.Is(out Response<UserFailure>? failure)) throw new UserOperationException(failure.Message.Code);
        return (response.Is(out Response<UsersFound>? success)
                ? success.Message
                : throw new InvalidOperationException("Unexpected Worker response")).Users
            .Select(x => new UserDto(x.Id, x.Name, x.Email)).ToArray();
    }
}