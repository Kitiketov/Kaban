using Kaban.Api.Features.Projects.Application.DTO;
using MediatR;

namespace Kaban.Api.Features.Projects.Application.CreateProject;

public sealed record CreateProjectCommand(int ActorId, int TeamId, string Name) : IRequest<ProjectDto>;
