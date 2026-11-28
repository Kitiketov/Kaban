using Kaban.Worker.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Kaban.Worker.Features.Teams;

public sealed class TeamAccess(KabanDatabaseContext db)
{
    public async Task<string?> EnsureTeamMemberAsync(int teamId, int actorId, CancellationToken ct)
    {
        var teamExists = await db.Teams.AsNoTracking().AnyAsync(x => x.Id == teamId, ct);
        if (!teamExists)
            return "team_not_found";

        return await EnsureMemberAsync(teamId, actorId, ct);
    }

    public async Task<string?> EnsureMemberAsync(int teamId, int actorId, CancellationToken ct)
    {
        var isMember = await db.UserTeams.AsNoTracking()
            .AnyAsync(x => x.TeamId == teamId && x.UserId == actorId, ct);
        return isMember ? null : "not_a_member";
    }
}