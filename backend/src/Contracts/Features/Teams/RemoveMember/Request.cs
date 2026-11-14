namespace Contracts.Features.Teams.RemoveMember;

public sealed record RemoveTeamMemberRequest(int ActorId, int TeamId, int UserId);
