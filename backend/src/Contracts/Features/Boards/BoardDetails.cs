namespace Contracts.Features.Boards;

public sealed record BoardDetails(int Id, int ProjectId, string Name, IReadOnlyList<BoardColumnDetails> Columns);