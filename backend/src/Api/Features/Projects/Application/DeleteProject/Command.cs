using MediatR;

namespace Kaban.Api.Features.Projects.Application.DeleteProject;

public sealed record DeleteProjectCommand(int ActorId, int ProjectId) : IRequest;
