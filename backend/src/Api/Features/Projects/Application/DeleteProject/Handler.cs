using Contracts.Features.Projects.Common;
using Contracts.Features.Projects.Delete;
using Kaban.Api.Common.Exceptions;
using MassTransit;
using MediatR;

namespace Kaban.Api.Features.Projects.Application.DeleteProject;

public sealed class DeleteProjectHandler(IRequestClient<DeleteProjectRequest> client)
    : IRequestHandler<DeleteProjectCommand>
{
    public async Task Handle(DeleteProjectCommand command, CancellationToken ct)
    {
        var response = await client.GetResponse<ProjectDeleted, ProjectFailure>(
            new(command.ActorId, command.ProjectId), ct);
        if (response.Is(out Response<ProjectFailure>? failure)) throw new ProjectOperationException(failure.Message.Code);
    }
}
