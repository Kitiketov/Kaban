using Contracts.Features.Projects.Common;
using Contracts.Features.Projects.Create;
using Kaban.Api.Common.Exceptions;
using Kaban.Api.Features.Projects.Application.DTO;
using MassTransit;
using MediatR;

namespace Kaban.Api.Features.Projects.Application.CreateProject;

public sealed class CreateProjectHandler(IRequestClient<CreateProjectRequest> client)
    : IRequestHandler<CreateProjectCommand, ProjectDto>
{
    public async Task<ProjectDto> Handle(CreateProjectCommand command, CancellationToken ct)
    {
        var response = await client.GetResponse<ProjectCreated, ProjectFailure>(
            new(command.ActorId, command.TeamId, command.Name.Trim()), ct);
        if (response.Is(out Response<ProjectFailure>? failure)) throw new ProjectOperationException(failure.Message.Code);
        var created = (response.Is(out Response<ProjectCreated>? success)
            ? success.Message
            : throw new InvalidOperationException("Unexpected Worker response"));
        return new(created.Id, created.TeamId, created.Name);
    }
}
