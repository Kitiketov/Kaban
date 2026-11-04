namespace Contracts.Features.Boards;

public sealed record CreateBoardRequest(int ProjectId, string Name);