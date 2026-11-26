using System.Net.Mail;
using Contracts.Features.Users;
using Kaban.Worker.Infrastructure.Database;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using UserEntity = Kaban.Worker.Features.Users.Domain.Users;

namespace Kaban.Worker.Features.Users;

public sealed class RegisterUserConsumer(KabanDatabaseContext db) : IConsumer<RegisterUserRequested>
{
    public async Task Consume(ConsumeContext<RegisterUserRequested> context)
    {
        var request = context.Message;
        if (
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
        var emailExists = false;
        UserRegistered? result = null;
        await using (var transaction = await db.Database.BeginTransactionAsync(ct))
        {
            // Serialize registrations for the same normalized email across Worker instances.
            await db.Database.AcquireResourceLockAsync(LockResource.UserEmail, request.Email, ct: ct);
            emailExists = await db.Set<UserEntity>().AnyAsync(x => x.Email == request.Email, ct);
            if (!emailExists)
            {
                var user = new UserEntity
                    { Email = request.Email, Name = request.Name.Trim(), HashedPassword = request.PasswordHash };
                db.Add(user);
                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                result = new UserRegistered(user.Id);
            }
        }

        if (emailExists)
        {
            await context.RespondAsync(new UserFailure("email_conflict"));
            return;
        }

        var userRegistered = result ?? throw new InvalidOperationException("User registration result was not created.");
        await context.RespondAsync(userRegistered);
    }
}
