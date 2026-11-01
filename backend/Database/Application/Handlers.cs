using Database.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Database.Application;

public interface IRecordWriter
{
    Task<TEntity?> CreateAsync<TEntity>(TEntity entity, CancellationToken ct = default) where TEntity : class, ITable;
    Task<TEntity?> UpdateAsync<TEntity>(object[] key, TEntity updatedEntity, CancellationToken ct = default) 
        where TEntity : class, ITable;
    Task<bool> DeleteAsync<TEntity>(object[] key, CancellationToken ct = default) where TEntity : class, ITable;
}

public sealed class RecordWriter(KabanDatabaseContext context) : IRecordWriter
{
    public async Task<TEntity?> CreateAsync<TEntity>(TEntity entity, CancellationToken ct = default) 
        where TEntity : class, ITable
    {
        await using var transaction = await context.Database.BeginTransactionAsync(ct);
        await context.Database.UseTransactionAsync(transaction.GetDbTransaction(), ct);
        
        try
        {
            await context.Set<TEntity>().AddAsync(entity, ct);
            await context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return entity;
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            return null;
        }
    }

    public async Task<TEntity?> UpdateAsync<TEntity>(object[] key, TEntity updateEntity, CancellationToken ct = default) 
        where TEntity : class, ITable
    {
        await using var transaction = await context.Database.BeginTransactionAsync(ct);
        await context.Database.UseTransactionAsync(transaction.GetDbTransaction(), ct);

        try
        {
            var entity = await context.Set<TEntity>().FindAsync(key, ct);
            if (entity is null)
            {
                await transaction.RollbackAsync(ct);
                return null;
            }
            await context.Set<TEntity>().AddAsync(updateEntity, ct);
            await context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return entity;
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync(ct);
            return null;
        }
    }

    public async Task<bool> DeleteAsync<TEntity>(object[] key, CancellationToken ct = default)
        where TEntity : class, ITable
    {
        await using var transaction = await context.Database.BeginTransactionAsync(ct);
        await context.Database.UseTransactionAsync(transaction.GetDbTransaction(), ct);

        try
        {
            var entity = await context.Set<TEntity>().FindAsync(key, ct);
            if (entity is null)
            {
                await transaction.RollbackAsync(ct);
                return false;
            }

            context.Set<TEntity>().Remove(entity);
            await context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return true;
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            return false;
        }
    }
}
