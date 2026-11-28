using Kaban.Api.Features.Teams.Application.DTO;
using MediatR;

namespace Kaban.Api.Features.Teams.Application.CreateTeamRole;

public sealed record CreateTeamRoleCommand(int ActorId, int TeamId, string Role) : IRequest<TeamRoleDto>;
