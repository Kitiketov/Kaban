namespace Contracts.Features.Boards.List;

public sealed record ListBoardsSuccess(IReadOnlyList<BoardSummary> Boards);

public sealed record BoardSummary(int Id, int ProjectId, string Name);
