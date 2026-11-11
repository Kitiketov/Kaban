using Contracts.Features.Teams.Common;
using Contracts.Features.Teams.Get;
using Kaban.Api.Common.Exceptions;
using Kaban.Api.Features.Teams.Application.DTO;
using MassTransit;
using MediatR;

namespace Kaban.Api.Features.Teams.Application.GetTeam;

public sealed class GetTeamHandler(IRequestClient<GetTeamRequest> client)
    : IRequestHandler<GetTeamQuery, TeamDetailsDto>
{
    public async Task<TeamDetailsDto> Handle(GetTeamQuery query, CancellationToken ct)
    {
        var response = await client.GetResponse<TeamDetails, TeamFailure>(
            new(query.ActorId, query.TeamId), ct);
        if (response.Is(out Response<TeamFailure>? failure)) throw new TeamOperationException(failure.Message.Code);
        var details = (response.Is(out Response<TeamDetails>? success)
            ? success.Message
            : throw new InvalidOperationException("Unexpected Worker response"));
        return new(
            details.Id,
            details.Name,
            details.Members.Select(m => new TeamMemberDetailsDto(m.UserId, m.Name, m.Email, m.Role)).ToArray());
    }
}
