namespace Contracts.Features.Boards.RenameColumn;

public sealed record RenameBoardColumnSuccess(int Id, int BoardId, string Name, int Order);