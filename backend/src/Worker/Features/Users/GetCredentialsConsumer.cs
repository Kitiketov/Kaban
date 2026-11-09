using System.Net.Mail;
using Kaban.Contracts.Users;
using Kaban.Worker.Infrastructure.Database;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using UserEntity = Kaban.Worker.Infrastructure.Database.Domain.Users;

namespace Kaban.Worker.Features.Users;

public sealed class GetCredentialsConsumer(KabanDatabaseContext db) : IConsumer<GetCredentialsRequested>
{
    public async Task Consume(ConsumeContext<GetCredentialsRequested> context)
    {
        var email = context.Message.Email;
        if (string.IsNullOrWhiteSpace(email) || email.Length > 320 || !MailAddress.TryCreate(email, out _))
        {
            await context.RespondAsync(new UserFailure("invalid_credentials"));
            return;
        }

        var user = await db.Set<UserEntity>().AsNoTracking().Where(x => x.Email == email.Trim().ToLowerInvariant())
            .Select(x => new CredentialsFound(x.Id, x.HashedPassword)).SingleOrDefaultAsync(context.CancellationToken);
        if (user is null) await context.RespondAsync(new UserFailure("invalid_credentials"));
        else await context.RespondAsync(user);
    }
}