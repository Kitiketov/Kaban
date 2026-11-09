using Kaban.Contracts.Users;
using Kaban.Worker.Infrastructure.Database;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using UserEntity = Kaban.Worker.Infrastructure.Database.Domain.Users;

namespace Kaban.Worker.Features.Users;

public sealed class GetUserConsumer(KabanDatabaseContext db) : IConsumer<GetUserRequested>
{
    public async Task Consume(ConsumeContext<GetUserRequested> context)
    {
        var user = await db.Set<UserEntity>().AsNoTracking().Where(x => x.Id == context.Message.UserId)
            .Select(x => new UserResult(x.Id, x.Name, x.Email)).SingleOrDefaultAsync(context.CancellationToken);
        if (user is null) 
            await context.RespondAsync(new UserFailure("not_found"));
        else await context.RespondAsync(user);
    }
}