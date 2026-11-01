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
    public static BoardTask Create(string name,
        string description,
        int priority,
        DateTime deadline,
        int boardId,
        int userId,
        int columnId,
        int order,
        int[] tagIds)
    {
        return new BoardTask(name, description, priority, deadline, boardId, userId, columnId, order, tagIds);
    }
}