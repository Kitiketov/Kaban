using Kaban.Worker.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Kaban.Worker.Features.Teams;

/// <summary>
///     Authorization extension point: verifies actor membership in a team.
///     Future role-based permission checks belong here, so consumers do not
///     duplicate access rules.
/// </summary>
public sealed class TeamAccess(KabanDatabaseContext db)
{
    /// <returns>null when the actor is a team member; otherwise a failure code.</returns>
    public async Task<string?> EnsureMemberAsync(int teamId, int actorId, CancellationToken ct)
    {
        var isMember = await db.UserTeams.AsNoTracking()
            .AnyAsync(x => x.TeamId == teamId && x.UserId == actorId, ct);
        return isMember ? null : "not_a_member";
    }
}