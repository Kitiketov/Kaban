namespace Contracts.Features.Boards.Create;

public sealed record CreateBoardSuccess(int Id, int ProjectId, string Name);