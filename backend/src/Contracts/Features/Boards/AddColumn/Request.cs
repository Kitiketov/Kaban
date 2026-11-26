namespace Contracts.Features.Boards.AddColumn;

public sealed record AddBoardColumnRequest(int BoardId, string Name);