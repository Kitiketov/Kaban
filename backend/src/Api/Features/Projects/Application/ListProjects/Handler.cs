using Contracts.Features.Projects.Common;
using Contracts.Features.Projects.List;
using Kaban.Api.Common.Exceptions;
using Kaban.Api.Features.Projects.Application.DTO;
using MassTransit;
using MediatR;

namespace Kaban.Api.Features.Projects.Application.ListProjects;

public sealed class ListProjectsHandler(IRequestClient<ListTeamProjectsRequest> client)
    : IRequestHandler<ListProjectsQuery, ProjectListDto>
{
    public async Task<ProjectListDto> Handle(ListProjectsQuery query, CancellationToken ct)
    {
        var response = await client.GetResponse<ProjectListResult, ProjectFailure>(
            new(query.ActorId, query.TeamId), ct);
        if (response.Is(out Response<ProjectFailure>? failure)) throw new ProjectOperationException(failure.Message.Code);
        var result = (response.Is(out Response<ProjectListResult>? success)
            ? success.Message
            : throw new InvalidOperationException("Unexpected Worker response"));
        return new(result.Projects.Select(p => new ProjectSummaryDto(p.Id, p.TeamId, p.Name)).ToArray());
    }
}
