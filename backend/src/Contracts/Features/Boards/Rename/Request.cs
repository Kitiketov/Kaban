namespace Contracts.Features.Boards.Rename;

public sealed record RenameBoardRequest(int BoardId, string Name);