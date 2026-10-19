namespace Kaban.Api.Common.Application;

// Один SaveChanges для всех репозиториев внутри одной операции.
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct);
}
