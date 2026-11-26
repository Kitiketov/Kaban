namespace Contracts.Features.Boards.RenameColumn;

public sealed record RenameBoardColumnRequest(int ColumnId, string Name);