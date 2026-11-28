namespace Contracts.Features.Teams.CreateRole;

public sealed record CreateTeamRoleRequest(int ActorId, int TeamId, string Role);
