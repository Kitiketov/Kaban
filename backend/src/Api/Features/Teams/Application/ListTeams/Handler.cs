using Contracts.Features.Teams.Common;
using Contracts.Features.Teams.List;
using Kaban.Api.Common.Exceptions;
using Kaban.Api.Features.Teams.Application.DTO;
using MassTransit;
using MediatR;

namespace Kaban.Api.Features.Teams.Application.ListTeams;

public sealed class ListTeamsHandler(IRequestClient<ListTeamsRequest> client)
    : IRequestHandler<ListTeamsQuery, TeamListDto>
{
    public async Task<TeamListDto> Handle(ListTeamsQuery query, CancellationToken ct)
    {
        var response = await client.GetResponse<TeamListResult, TeamFailure>(new(query.ActorId), ct);
        if (response.Is(out Response<TeamFailure>? failure)) throw new TeamOperationException(failure.Message.Code);
        var result = (response.Is(out Response<TeamListResult>? success)
            ? success.Message
            : throw new InvalidOperationException("Unexpected Worker response"));
        return new(result.Teams.Select(t => new TeamSummaryDto(t.Id, t.Name, t.Role)).ToArray());
    }
}
