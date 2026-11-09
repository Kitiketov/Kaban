namespace Contracts.Features.Boards;

public sealed record BoardColumnRenamed(int Id, int BoardId, string Name, int Order);