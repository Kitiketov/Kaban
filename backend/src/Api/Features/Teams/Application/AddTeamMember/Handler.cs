using Contracts.Features.Teams.AddMember;
using Contracts.Features.Teams.Common;
using Kaban.Api.Common.Exceptions;
using Kaban.Api.Features.Teams.Application.DTO;
using MassTransit;
using MediatR;

namespace Kaban.Api.Features.Teams.Application.AddTeamMember;

public sealed class AddTeamMemberHandler(IRequestClient<AddTeamMemberRequest> client)
    : IRequestHandler<AddTeamMemberCommand, TeamMembershipDto>
{
    public async Task<TeamMembershipDto> Handle(AddTeamMemberCommand command, CancellationToken ct)
    {
        var response = await client.GetResponse<TeamMemberAdded, TeamFailure>(
            new(command.ActorId, command.TeamId, command.UserId), ct);
        if (response.Is(out Response<TeamFailure>? failure)) throw new TeamOperationException(failure.Message.Code);
        var added = (response.Is(out Response<TeamMemberAdded>? success)
            ? success.Message
            : throw new InvalidOperationException("Unexpected Worker response"));
        return new(added.TeamId, added.UserId, added.Role);
    }
}
