using Kaban.Api.Features.Users.Application.DTO;
using MediatR;

namespace Kaban.Api.Features.Users.Application.Search;

public sealed record SearchUsersQuery(int ActorId, string Query, int Limit) : IRequest<UserDto[]>;
