using Contracts.Features.Teams.Common;
using Contracts.Features.Teams.Delete;
using Kaban.Api.Common.Exceptions;
using MassTransit;
using MediatR;

namespace Kaban.Api.Features.Teams.Application.DeleteTeam;

public sealed class DeleteTeamHandler(IRequestClient<DeleteTeamRequest> client)
    : IRequestHandler<DeleteTeamCommand>
{
    public async Task Handle(DeleteTeamCommand command, CancellationToken ct)
    {
        var response = await client.GetResponse<TeamDeleted, TeamFailure>(
            new(command.ActorId, command.TeamId), ct);
        if (response.Is(out Response<TeamFailure>? failure)) throw new TeamOperationException(failure.Message.Code);
    }
}
