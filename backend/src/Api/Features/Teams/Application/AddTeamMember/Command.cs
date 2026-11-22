using Kaban.Api.Features.Teams.Application.DTO;
using MediatR;

namespace Kaban.Api.Features.Teams.Application.AddTeamMember;

public sealed record AddTeamMemberCommand(int ActorId, int TeamId, int UserId) : IRequest<TeamMembershipDto>;
