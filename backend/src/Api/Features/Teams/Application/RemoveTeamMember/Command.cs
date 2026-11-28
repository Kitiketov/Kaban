using MediatR;

namespace Kaban.Api.Features.Teams.Application.RemoveTeamMember;

public sealed record RemoveTeamMemberCommand(int ActorId, int TeamId, int UserId) : IRequest;
