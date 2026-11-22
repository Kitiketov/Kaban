namespace Contracts.Features.Teams.ChangeMemberRole;

public sealed record ChangeMemberRoleRequest(int ActorId, int TeamId, int UserId, string Role);
