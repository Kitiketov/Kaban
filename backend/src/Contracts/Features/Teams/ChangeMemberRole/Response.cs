namespace Contracts.Features.Teams.ChangeMemberRole;

public sealed record MemberRoleChanged(int TeamId, int UserId, string Role);
