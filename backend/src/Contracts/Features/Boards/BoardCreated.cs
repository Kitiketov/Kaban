namespace Contracts.Features.Boards;

public sealed record BoardCreated(int Id, int ProjectId, string Name);