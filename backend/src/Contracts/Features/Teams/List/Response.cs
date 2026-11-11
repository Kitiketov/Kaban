namespace Contracts.Features.Teams.List;

public sealed record TeamSummary(int Id, string Name, string Role);
public sealed record TeamListResult(IReadOnlyList<TeamSummary> Teams);
