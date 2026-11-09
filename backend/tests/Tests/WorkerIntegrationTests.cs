using Contracts.Features.Users;
using Kaban.Worker.Features.Users;
using Kaban.Worker.Infrastructure.Database;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;
using UserEntity = Kaban.Worker.Infrastructure.Database.Domain.Users;

namespace Kaban.Tests;

public sealed class ExternalServicesFactAttribute : FactAttribute
{
    public ExternalServicesFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("KABAN_TEST_POSTGRES")) ||
            string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("KABAN_TEST_RABBITMQ")))
            Skip = "Requires isolated PostgreSQL and RabbitMQ: KABAN_TEST_POSTGRES and KABAN_TEST_RABBITMQ.";
    }
}

public sealed class WorkerIntegrationTests
{
    [ExternalServicesFact]
    public async Task Registration_Commits_RejectsDuplicateEmails_HandlesLateSuccess_AndFaults()
    {
        var database = "kaban_test_" + Guid.NewGuid().ToString("N");
        var connection = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("KABAN_TEST_POSTGRES"));
        await using var admin = new NpgsqlConnection(connection.ConnectionString);
        await admin.OpenAsync();
        await new NpgsqlCommand($"CREATE DATABASE {database}", admin).ExecuteNonQueryAsync();
        connection.Database = database;
        try
        {
            var options = new DbContextOptionsBuilder<KabanDatabaseContext>()
                .UseNpgsql(connection.ConnectionString).Options;
            await using var db = new KabanDatabaseContext(options);
            await db.Database.MigrateAsync();

            var queue = "kaban-test-" + Guid.NewGuid().ToString("N");
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDbContext<KabanDatabaseContext>(x =>
                x.UseNpgsql(connection.ConnectionString));
            services.AddMassTransit(x =>
            {
                x.AddConsumer<RegisterUserConsumer>();
                x.AddConsumer<GetUserConsumer>();
                x.AddRequestClient<RegisterUserRequested>(new Uri("queue:" + queue), RequestTimeout.After(s: 3));
                x.AddRequestClient<GetUserRequested>(new Uri("queue:" + queue), RequestTimeout.After(s: 3));
                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(new Uri(Environment.GetEnvironmentVariable("KABAN_TEST_RABBITMQ")!));
                    cfg.ReceiveEndpoint(queue, endpoint =>
                    {
                        endpoint.Durable = false;
                        endpoint.AutoDelete = true;
                        endpoint.ConfigureConsumer<RegisterUserConsumer>(context);
                        endpoint.ConfigureConsumer<GetUserConsumer>(context);
                    });
                });
            });
            await using var provider = services.BuildServiceProvider(true);
            var bus = provider.GetRequiredService<IBusControl>();
            await bus.StartAsync();
            try
            {
                var client = provider.GetRequiredService<IRequestClient<RegisterUserRequested>>();
                var request = new RegisterUserRequested("user@example.com", "User", "test-hash");
                var duplicates = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ =>
                    client.GetResponse<UserRegistered, UserFailure>(request)));
                Assert.Equal(1, duplicates.Count(r => r.Is(out Response<UserRegistered>? _)));
                Assert.Equal(3, duplicates.Count(r => r.Is(out Response<UserFailure>? failure) && failure.Message.Code == "email_conflict"));
                Assert.Equal(1, await db.Set<UserEntity>().CountAsync());
                var late = request with { Email = "late@example.com" };
                await using (var blocker = new NpgsqlConnection(connection.ConnectionString))
                {
                    await blocker.OpenAsync();
                    await using var transaction = await blocker.BeginTransactionAsync();
                    await using var command = new NpgsqlCommand(
                        "SELECT pg_advisory_xact_lock(hashtextextended(@email, 1))", blocker, transaction);
                    command.Parameters.AddWithValue("email", late.Email);
                    await command.ExecuteNonQueryAsync();
                    await Assert.ThrowsAsync<RequestTimeoutException>(() => client.GetResponse<UserRegistered, UserFailure>(late));
                    await transaction.CommitAsync();
                }
                var retry = await client.GetResponse<UserRegistered, UserFailure>(late);
                Assert.True(retry.Is(out Response<UserFailure>? failure));
                Assert.Equal("email_conflict", failure!.Message.Code);
                Assert.Equal(1, await db.Set<UserEntity>().CountAsync(x => x.Email == late.Email));

                // Damage only the database created by this test, to exercise a genuine DB fault.
                await db.Database.ExecuteSqlRawAsync("ALTER TABLE users RENAME TO users_unavailable");
                var reader = provider.GetRequiredService<IRequestClient<GetUserRequested>>();
                await Assert.ThrowsAsync<RequestFaultException>(() => reader.GetResponse<UserResult, UserFailure>(new(1)));
            }
            finally { await bus.StopAsync(); }
        }
        finally
        {
            NpgsqlConnection.ClearAllPools();
            // The identifier is generated above; no user-supplied database is ever dropped.
            await new NpgsqlCommand($"DROP DATABASE {database} WITH (FORCE)", admin).ExecuteNonQueryAsync();
        }
    }
}
