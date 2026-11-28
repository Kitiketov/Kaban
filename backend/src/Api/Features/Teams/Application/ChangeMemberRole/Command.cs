using Kaban.Api.Features.Teams.Application.DTO;
using MediatR;

namespace Kaban.Api.Features.Teams.Application.ChangeMemberRole;

public sealed record ChangeMemberRoleCommand(int ActorId, int TeamId, int UserId, string Role)
    : IRequest<TeamMembershipDto>;
