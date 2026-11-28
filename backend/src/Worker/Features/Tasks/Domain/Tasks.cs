using Kaban.Worker.Features.Boards.Domain;

namespace Kaban.Worker.Features.Tasks.Domain;

public class Tasks
{
    public Tasks() { }

    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public PriorityEnum Priority { get; set; } = PriorityEnum.NoPriority;
    public DateTime Created { get; set; }
    public DateTime? Deadline { get; set; }
    public string TaskRank { get; set; } = null!;

    public int BoardColumnId { get; set; }
    public BoardColumns? BoardColumn { get; set; }

    public static Tasks Create(
        string name,
        int columnId,
        string rank,
        string? description = null,
        PriorityEnum priority = PriorityEnum.NoPriority,
        DateTime? deadline = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(rank);

        return new Tasks
        {
            Name = name.Trim(),
            BoardColumnId = columnId,
            TaskRank = rank,
            Description = description,
            Priority = priority,
            Deadline = deadline,
            Created = DateTime.UtcNow
        };
    }

    public void UpdateDetails(
        string name,
        string? description,
        PriorityEnum priority,
        DateTime? deadline)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name.Trim();
        Description = description;
        Priority = priority;
        Deadline = deadline;
    }

    public void MoveToColumn(int newColumnId, string newRank)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newRank);
        BoardColumnId = newColumnId;
        TaskRank = newRank;
    }

    public void SetRank(string newRank)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newRank);
        TaskRank = newRank;
    }
}
