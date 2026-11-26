using System.Net.Mail;
using Contracts.Features.Users.Common;
using Contracts.Features.Users.Register;
using Kaban.Worker.Infrastructure.Database;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using UserEntity = Kaban.Worker.Features.Users.Domain.Users;

namespace Kaban.Worker.Features.Users;

public sealed class RegisterUserConsumer(KabanDatabaseContext db) : IConsumer<RegisterUserRequest>
{
    public async Task Consume(ConsumeContext<RegisterUserRequest> context)
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
        RegisterUserSuccess? result = null;
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
                result = new RegisterUserSuccess(user.Id);
            }
        }

        if (emailExists)
        {
            await context.RespondAsync(new UserFailure("email_conflict"));
            return;
        }

        await context.RespondAsync(result
            ?? throw new InvalidOperationException("User registration result was not created."));
    }
}
