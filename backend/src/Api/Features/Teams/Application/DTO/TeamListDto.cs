namespace Kaban.Api.Features.Teams.Application.DTO;

public sealed record TeamSummaryDto(int Id, string Name, string Role);
public sealed record TeamListDto(IReadOnlyList<TeamSummaryDto> Teams);
