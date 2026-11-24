namespace Contracts.Features.Boards.AddColumn;

public sealed record AddBoardColumnSuccess(int Id, int BoardId, string Name, int Order);