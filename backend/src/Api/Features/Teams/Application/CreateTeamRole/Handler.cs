using Contracts.Features.Teams.Common;
using Contracts.Features.Teams.CreateRole;
using Kaban.Api.Common.Exceptions;
using Kaban.Api.Features.Teams.Application.DTO;
using MassTransit;
using MediatR;

namespace Kaban.Api.Features.Teams.Application.CreateTeamRole;

public sealed class CreateTeamRoleHandler(IRequestClient<CreateTeamRoleRequest> client)
    : IRequestHandler<CreateTeamRoleCommand, TeamRoleDto>
{
    public async Task<TeamRoleDto> Handle(CreateTeamRoleCommand command, CancellationToken ct)
    {
        var response = await client.GetResponse<TeamRoleCreated, TeamFailure>(
            new(command.ActorId, command.TeamId, command.Role.Trim()), ct);
        if (response.Is(out Response<TeamFailure>? failure)) throw new TeamOperationException(failure.Message.Code);
        var created = (response.Is(out Response<TeamRoleCreated>? success)
            ? success.Message
            : throw new InvalidOperationException("Unexpected Worker response"));
        return new(created.TeamId, created.Role);
    }
}
