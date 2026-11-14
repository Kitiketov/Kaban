namespace Kaban.Api.Features.Teams.DTO;

public sealed record CreateTeamDto(string Name);
public sealed record AddTeamMemberDto(int UserId);
public sealed record CreateTeamRoleDto(string Role);
public sealed record ChangeMemberRoleDto(string Role);