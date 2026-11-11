namespace Kaban.Api.Features.Teams.Application.DTO;

public sealed record TeamMemberDetailsDto(int UserId, string Name, string Email, string Role);
public sealed record TeamDetailsDto(int Id, string Name, IReadOnlyList<TeamMemberDetailsDto> Members);
