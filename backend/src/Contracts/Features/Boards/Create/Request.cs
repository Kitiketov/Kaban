namespace Contracts.Features.Boards.Create;

public sealed record CreateBoardRequest(int ProjectId, string Name);
