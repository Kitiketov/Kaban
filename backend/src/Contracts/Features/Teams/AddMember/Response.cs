namespace Contracts.Features.Teams.AddMember;

public sealed record TeamMemberAdded(int TeamId, int UserId, string Role);
