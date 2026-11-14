namespace Contracts.Features.Projects.List;

public sealed record ProjectSummary(int Id, int TeamId, string Name);
public sealed record ProjectListResult(IReadOnlyList<ProjectSummary> Projects);
