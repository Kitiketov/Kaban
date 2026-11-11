using Contracts.Features.Teams.ChangeMemberRole;
using Contracts.Features.Teams.Common;
using Kaban.Api.Common.Exceptions;
using Kaban.Api.Features.Teams.Application.DTO;
using MassTransit;
using MediatR;

namespace Kaban.Api.Features.Teams.Application.ChangeMemberRole;

public sealed class ChangeMemberRoleHandler(IRequestClient<ChangeMemberRoleRequest> client)
    : IRequestHandler<ChangeMemberRoleCommand, TeamMembershipDto>
{
    public async Task<TeamMembershipDto> Handle(ChangeMemberRoleCommand command, CancellationToken ct)
    {
        var response = await client.GetResponse<MemberRoleChanged, TeamFailure>(
            new(command.ActorId, command.TeamId, command.UserId, command.Role.Trim()), ct);
        if (response.Is(out Response<TeamFailure>? failure)) throw new TeamOperationException(failure.Message.Code);
        var changed = (response.Is(out Response<MemberRoleChanged>? success)
            ? success.Message
            : throw new InvalidOperationException("Unexpected Worker response"));
        return new(changed.TeamId, changed.UserId, changed.Role);
    }
}
