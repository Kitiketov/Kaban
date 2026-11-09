using Kaban.Api.Features.Users.Application.DTO;
using MediatR;

namespace Kaban.Api.Features.Users.Application.GetMe;

public sealed record GetMeQuery(int UserId) : IRequest<UserDto>;