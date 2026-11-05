using System.Net.Mail;
using Kaban.Contracts.Users;
using Kaban.Worker.Infrastructure.Database;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using UserEntity = Kaban.Worker.Infrastructure.Database.Domain.Users;

namespace Kaban.Worker.Features.Users;

public sealed class RegisterUserConsumer(KabanDatabaseContext db) : IConsumer<RegisterUserRequested>
{
    public async Task Consume(ConsumeContext<RegisterUserRequested> context)
    {
        var request = context.Message;
        if (request.OperationId == Guid.Empty || request.Fingerprint?.Length != 64 ||
            string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 64 ||
            string.IsNullOrWhiteSpace(request.Email) || request.Email.Length > 320 ||
            !MailAddress.TryCreate(request.Email, out var address) || address.Address != request.Email ||
            request.Email != request.Email.Trim().ToLowerInvariant() ||
            string.IsNullOrWhiteSpace(request.PasswordHash) || request.PasswordHash.Length > 255)
        {
            await context.RespondAsync(new UserFailure("invalid_request"));
            return;
        }

        var ct = context.CancellationToken;
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        // Transaction-scoped locks serialize both duplicate operation IDs and equal normalized emails,
        // including requests handled concurrently by different Worker instances.
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({request.OperationId.ToString()}, 0))", ct);
        var previous = await db.Set<RegistrationOperation>().SingleOrDefaultAsync(x => x.Id == request.OperationId, ct);
        if (previous is not null)
        {
            await transaction.CommitAsync(ct);
            if (previous.Fingerprint != request.Fingerprint)
                await context.RespondAsync(new UserFailure("idempotency_conflict"));
            else
                await context.RespondAsync(new UserRegistered(previous.UserId));
            return;
        }

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({request.Email}, 1))", ct);
        if (await db.Set<UserEntity>().AnyAsync(x => x.Email == request.Email, ct))
        {
            await transaction.RollbackAsync(ct);
            await context.RespondAsync(new UserFailure("email_conflict"));
            return;
        }

        var user = new UserEntity
            { Email = request.Email, Name = request.Name.Trim(), HashedPassword = request.PasswordHash };
        db.Add(user);
        await db.SaveChangesAsync(ct);
        db.Add(new RegistrationOperation
        {
            Id = request.OperationId, Fingerprint = request.Fingerprint, UserId = user.Id, CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        await context.RespondAsync(new UserRegistered(user.Id));
    }
}