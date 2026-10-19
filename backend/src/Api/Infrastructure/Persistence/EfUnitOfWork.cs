using Kaban.Api.Common.Application;

namespace Kaban.Api.Infrastructure.Persistence;

public sealed class EfUnitOfWork(KabanDbContext db) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
