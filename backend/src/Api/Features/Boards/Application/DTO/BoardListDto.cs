namespace Kaban.Api.Features.Boards.Application.DTO;

public sealed record BoardListDto(IReadOnlyList<BoardDto> Boards);