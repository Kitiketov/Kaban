namespace Contracts.Features.Boards;

public sealed record BoardListResult(IReadOnlyList<BoardSummary> Boards);