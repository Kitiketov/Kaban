namespace Contracts.Features.Teams.AddMember;

public sealed record AddTeamMemberRequest(int ActorId, int TeamId, int UserId);
