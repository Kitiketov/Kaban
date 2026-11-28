using Kaban.Worker.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Kaban.Worker.Features.Boards;

public sealed class BoardAccess(KabanDatabaseContext db)
{
    public async Task<bool> ColumnExistsOnBoardAsync(int boardId, int columnId, CancellationToken ct)
    {
        return await db.BoardColumns.AsNoTracking()
            .AnyAsync(x => x.Id == columnId && x.BoardId == boardId, ct);
    }

    public async Task<int?> GetBoardIdByColumnAsync(int columnId, CancellationToken ct)
    {
        return await db.BoardColumns.AsNoTracking()
            .Where(x => x.Id == columnId)
            .Select(x => (int?)x.BoardId)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<int?> GetCommonBoardIdAsync(int sourceColumnId, int targetColumnId, CancellationToken ct)
    {
        if (sourceColumnId == targetColumnId)
            return await GetBoardIdByColumnAsync(sourceColumnId, ct);

        var columns = await db.BoardColumns.AsNoTracking()
            .Where(x => x.Id == sourceColumnId || x.Id == targetColumnId)
            .Select(x => new { x.Id, x.BoardId })
            .ToListAsync(ct);

        if (columns.Count < 2)
            return null;

        return columns[0].BoardId == columns[1].BoardId ? columns[0].BoardId : null;
    }

    public async Task<bool> HasUserAccessAsync(int userId, int boardId, CancellationToken ct)
    {
        return await (
            from board in db.Boards.AsNoTracking()
            where board.Id == boardId
            join tp in db.TeamProjects.AsNoTracking() on board.ProjectId equals tp.ProjectId
            join ut in db.UserTeams.AsNoTracking() on tp.TeamId equals ut.TeamId
            where ut.UserId == userId
            select ut.UserId
        ).AnyAsync(ct);
    }
}
