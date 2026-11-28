namespace Kaban.Api.Features.Projects.Application.DTO;

public sealed record ProjectSummaryDto(int Id, int TeamId, string Name);
public sealed record ProjectListDto(IReadOnlyList<ProjectSummaryDto> Projects);
