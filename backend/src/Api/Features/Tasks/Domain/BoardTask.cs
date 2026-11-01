namespace Kaban.Api.Features.Tasks.Domain;

public sealed record BoardTask(
    string Name,
    string Description,
    int Priority,
    DateTime Deadline,
    int BoardId,
    int UserId,
    int ColumnId,
    int Order,
    int[] TagIds)
{
    public static BoardTask Create(string Name,
        string Description,
        int Priority,
        DateTime Deadline,
        int BoardId,
        int UserId,
        int ColumnId,
        int Order,
        int[] TagIds)
    {
        return new BoardTask(Name, Description, Priority, Deadline, BoardId, UserId, ColumnId, Order, TagIds);
    }
}