namespace Contracts.Features.Boards;

public sealed record RenameBoardRequest(int BoardId, string Name);