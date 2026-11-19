using Kaban.Api.Features.Projects.Application.DTO;
using MediatR;

namespace Kaban.Api.Features.Projects.Application.ListProjects;

public sealed record ListProjectsQuery(int ActorId, int TeamId) : IRequest<ProjectListDto>;
