namespace Contracts.Features.Boards;

public sealed record RenameBoardColumnRequest(int ColumnId, string Name);