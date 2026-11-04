namespace Kaban.Api.Features.Boards.DTO;

public sealed record BoardListDto(IReadOnlyList<BoardDto> Boards);