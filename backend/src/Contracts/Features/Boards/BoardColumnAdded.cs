namespace Contracts.Features.Boards;

public sealed record BoardColumnAdded(int Id, int BoardId, string Name, int Order);