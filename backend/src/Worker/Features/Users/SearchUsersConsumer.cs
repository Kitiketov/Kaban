using Contracts.Features.Users;
using Kaban.Worker.Infrastructure.Database;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using UserEntity = Kaban.Worker.Infrastructure.Database.Domain.Users;

namespace Kaban.Worker.Features.Users;

public sealed class SearchUsersConsumer(KabanDatabaseContext db) : IConsumer<SearchUsersRequested>
{
    public async Task Consume(ConsumeContext<SearchUsersRequested> context)
    {
        var request = context.Message;
        if (request.ActorId <= 0 || string.IsNullOrWhiteSpace(request.Query) ||
            request.Query.Length > 320 || request.Limit is < 1 or > 100)
        {
            await context.RespondAsync(new UserFailure("invalid_request"));
            return;
        }

        var users = db.Set<UserEntity>().AsNoTracking();
        if (!await users.AnyAsync(x => x.Id == request.ActorId, context.CancellationToken))
        {
            await context.RespondAsync(new UserFailure("unauthorized"));
            return;
        }

        var query = request.Query.Trim().ToLowerInvariant();
        var results = await users.Where(x => x.Name.ToLower().Contains(query) || x.Email.ToLower().Contains(query))
            .OrderBy(x => x.Name).ThenBy(x => x.Id).Take(request.Limit)
            .Select(x => new UserResult(x.Id, x.Name, x.Email)).ToArrayAsync(context.CancellationToken);
        await context.RespondAsync(new UsersFound(results));
    }
}