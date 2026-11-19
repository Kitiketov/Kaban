using Kaban.Api.Features.Projects.Application.DTO;
using MediatR;

namespace Kaban.Api.Features.Projects.Application.GetProject;

public sealed record GetProjectQuery(int ActorId, int ProjectId) : IRequest<ProjectDto>;
