namespace Contracts.Features.Projects.Create;

public sealed record CreateProjectRequest(int ActorId, int TeamId, string Name);
