using System.Net.Mail;
using Contracts.Features.Users.Common;
using Contracts.Features.Users.GetCredentials;
using Kaban.Worker.Infrastructure.Database;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using UserEntity = Kaban.Worker.Features.Users.Domain.Users;

namespace Kaban.Worker.Features.Users;

public sealed class GetCredentialsConsumer(KabanDatabaseContext db) : IConsumer<GetCredentialsRequest>
{
    public async Task Consume(ConsumeContext<GetCredentialsRequest> context)
    {
        var email = context.Message.Email;
        if (string.IsNullOrWhiteSpace(email) || email.Length > 320 || !MailAddress.TryCreate(email, out _))
        {
            await context.RespondAsync(new UserFailure("invalid_credentials"));
            return;
        }

        var credentials = await db.Set<UserEntity>().AsNoTracking().Where(x => x.Email == email.Trim().ToLowerInvariant())
            .Select(x => new GetCredentialsSuccess(x.Id, x.HashedPassword)).SingleOrDefaultAsync(context.CancellationToken);
        if (credentials is null) await context.RespondAsync(new UserFailure("invalid_credentials"));
        else await context.RespondAsync(credentials);
    }
}
