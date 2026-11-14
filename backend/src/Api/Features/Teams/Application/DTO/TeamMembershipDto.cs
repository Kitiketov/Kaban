namespace Kaban.Api.Features.Teams.Application.DTO;

public sealed record TeamMembershipDto(int TeamId, int UserId, string Role);
