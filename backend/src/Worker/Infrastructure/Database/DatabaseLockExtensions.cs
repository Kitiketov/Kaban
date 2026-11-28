using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Kaban.Worker.Infrastructure.Database;

public enum LockResource
{
    BoardColumn = 1,
    Team = 2,
    UserEmail = 3,
    Board = 4,
}

public static class DatabaseLockExtensions
{
    extension(DatabaseFacade database)
    {
        public Task AcquireResourceLockAsync(LockResource resource,
            int resourceId,
            CancellationToken ct = default)
        {
            return database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock({(int)resource}, {resourceId})", ct);
        }

        public Task AcquireResourceLockAsync(LockResource resource,
            string textKey,
            CancellationToken ct = default)
        {
            return database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock({(int)resource}, hashtext({textKey}))", ct);
        }
    }
}
