namespace Contracts.Features.Boards;

public sealed record AddBoardColumnRequest(int BoardId, string Name);