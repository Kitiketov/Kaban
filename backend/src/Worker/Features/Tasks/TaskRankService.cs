using Contracts.Features.Tasks.Common;
using Kaban.Worker.Infrastructure.Database;
using LexoAlgorithm;
using Microsoft.EntityFrameworkCore;
using OneOf;

namespace Kaban.Worker.Features.Tasks;

public sealed class TaskRankService(KabanDatabaseContext db)
{
    public async Task<string> GetNextColumnRankAsync(int columnId, CancellationToken ct)
    {
        var maxRankStr = await db.Tasks.AsNoTracking()
            .Where(t => t.BoardColumnId == columnId)
            .Select(t => (string?)t.TaskRank)
            .MaxAsync(ct);

        if (maxRankStr is null)
        {
            return LexoRank.Middle().ToString();
        }

        var maxRank = LexoRank.Parse(maxRankStr);
        return maxRank.GenNext().ToString();
    }

    public async Task<OneOf<string, ITaskFailed>> CalculateMoveRankAsync(
        int targetColumnId,
        int? prevTaskId,
        int? nextTaskId,
        CancellationToken ct)
    {
        string? prevRank = null;
        string? nextRank = null;

        if (prevTaskId.HasValue)
        {
            var prev = await db.Tasks.AsNoTracking()
                .Where(t => t.Id == prevTaskId.Value)
                .Select(t => new { t.BoardColumnId, t.TaskRank })
                .FirstOrDefaultAsync(ct);
            if (prev is null)
                return new TaskNotFound(prevTaskId.Value);

            if (prev.BoardColumnId != targetColumnId)
                return new TaskNotInColumn(prev.BoardColumnId, targetColumnId);

            prevRank = prev.TaskRank;
        }

        if (!nextTaskId.HasValue) return CalculateRank(prevRank, nextRank);

        var next = await db.Tasks.AsNoTracking()
            .Where(t => t.Id == nextTaskId.Value)
            .Select(t => new { t.BoardColumnId, t.TaskRank })
            .FirstOrDefaultAsync(ct);

        if (next is null)
            return new TaskNotFound(nextTaskId.Value);

        if (next.BoardColumnId != targetColumnId)
            return new TaskNotInColumn(next.BoardColumnId, targetColumnId);

        nextRank = next.TaskRank;

        return CalculateRank(prevRank, nextRank);
    }

    private static string CalculateRank(string? prevRank, string? nextRank)
    {
        if (prevRank is null && nextRank is null)
            return LexoRank.Middle().ToString();

        if (prevRank is not null && nextRank is not null)
            return LexoRank.Parse(prevRank).Between(LexoRank.Parse(nextRank)).ToString();

        if (prevRank is not null)
            return LexoRank.Parse(prevRank).GenNext().ToString();

        return LexoRank.Parse(nextRank!).GenPrev().ToString();
    }
}
