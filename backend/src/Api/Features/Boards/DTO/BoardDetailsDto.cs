namespace Kaban.Api.Features.Boards.DTO;

public sealed record BoardDetailsDto(int Id, int ProjectId, string Name, IReadOnlyList<BoardColumnDetailsDto> Columns);