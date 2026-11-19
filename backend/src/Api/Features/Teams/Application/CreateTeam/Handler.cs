using Contracts.Features.Teams.Common;
using Contracts.Features.Teams.Create;
using Kaban.Api.Common.Exceptions;
using Kaban.Api.Features.Teams.Application.DTO;
using MassTransit;
using MediatR;

namespace Kaban.Api.Features.Teams.Application.CreateTeam;

public sealed class CreateTeamHandler(IRequestClient<CreateTeamRequest> client)
    : IRequestHandler<CreateTeamCommand, TeamDto>
{
    public async Task<TeamDto> Handle(CreateTeamCommand command, CancellationToken ct)
    {
        var response = await client.GetResponse<TeamCreated, TeamFailure>(
            new(command.ActorId, command.Name.Trim()), ct);
        if (response.Is(out Response<TeamFailure>? failure)) throw new TeamOperationException(failure.Message.Code);
        var created = (response.Is(out Response<TeamCreated>? success)
            ? success.Message
            : throw new InvalidOperationException("Unexpected Worker response"));
        return new(created.Id, created.Name);
    }
}
