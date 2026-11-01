namespace Contracts.Tasks;

public sealed record TaskCreateEvent(
    string Name,
    string Description,
    int Priority,
    DateTime Deadline,
    int BoardId,
    int UserId,
    int ColumnId,
    int Order,
    int[] TagIds);