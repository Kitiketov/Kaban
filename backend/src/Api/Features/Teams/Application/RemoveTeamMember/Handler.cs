using Contracts.Features.Teams.Common;
using Contracts.Features.Teams.RemoveMember;
using Kaban.Api.Common.Exceptions;
using MassTransit;
using MediatR;

namespace Kaban.Api.Features.Teams.Application.RemoveTeamMember;

public sealed class RemoveTeamMemberHandler(IRequestClient<RemoveTeamMemberRequest> client)
    : IRequestHandler<RemoveTeamMemberCommand>
{
    public async Task Handle(RemoveTeamMemberCommand command, CancellationToken ct)
    {
        var response = await client.GetResponse<TeamMemberRemoved, TeamFailure>(
            new RemoveTeamMemberRequest(command.ActorId, command.TeamId, command.UserId), ct);
        if (response.Is(out Response<TeamFailure>? failure)) 
            throw new TeamOperationException(failure.Message.Code);
    }
}
