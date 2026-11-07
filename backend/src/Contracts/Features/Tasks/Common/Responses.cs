namespace Contracts.Features.Tasks.Common;

public sealed record CreateTaskSuccess
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required int Priority { get; init; }
    public DateTime? Deadline { get; init; }
    public required int Order { get; init; }
    public required int ColumnId { get; init; }
}
