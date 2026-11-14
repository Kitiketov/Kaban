using Contracts.Features.Projects.Common;
using Contracts.Features.Projects.Get;
using Kaban.Api.Common.Exceptions;
using Kaban.Api.Features.Projects.Application.DTO;
using MassTransit;
using MediatR;

namespace Kaban.Api.Features.Projects.Application.GetProject;

public sealed class GetProjectHandler(IRequestClient<GetProjectRequest> client)
    : IRequestHandler<GetProjectQuery, ProjectDto>
{
    public async Task<ProjectDto> Handle(GetProjectQuery query, CancellationToken ct)
    {
        var response = await client.GetResponse<ProjectDetails, ProjectFailure>(
            new(query.ActorId, query.ProjectId), ct);
        if (response.Is(out Response<ProjectFailure>? failure)) throw new ProjectOperationException(failure.Message.Code);
        var details = (response.Is(out Response<ProjectDetails>? success)
            ? success.Message
            : throw new InvalidOperationException("Unexpected Worker response"));
        return new(details.Id, details.TeamId, details.Name);
    }
}
