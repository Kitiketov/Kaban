namespace Contracts.Features.Teams.Get;

public sealed record TeamMemberDetails(int UserId, string Name, string Email, string Role);
public sealed record TeamDetails(int Id, string Name, IReadOnlyList<TeamMemberDetails> Members);
