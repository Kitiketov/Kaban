using Kaban.Contracts.Users;
using Kaban.Worker.Features.Users;
using Kaban.Worker.Infrastructure.Database;
using Kaban.Worker.Infrastructure.Database.Domain;
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
    public async Task Registration_Commits_Deduplicates_HandlesLateSuccess_AndFaults()
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
                var request = new RegisterUserRequested(Guid.NewGuid(), new string('A', 64),
                    "user@example.com", "User", "test-hash");
                var duplicates = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ =>
                    client.GetResponse<UserRegistered, UserFailure>(request)));
                Assert.All(duplicates, r => Assert.True(r.Is(out Response<UserRegistered>? _)));
                Assert.Equal(1, await db.Set<UserEntity>().CountAsync());
                Assert.Equal(1, await db.Set<RegistrationOperation>().CountAsync());
                var conflict = await client.GetResponse<UserRegistered, UserFailure>(request with { Fingerprint = new string('B', 64) });
                Assert.True(conflict.Is(out Response<UserFailure>? rejected));
                Assert.Equal("idempotency_conflict", rejected!.Message.Code);
                var emailConflict = await client.GetResponse<UserRegistered, UserFailure>(request with { OperationId = Guid.NewGuid() });
                Assert.True(emailConflict.Is(out Response<UserFailure>? duplicateEmail));
                Assert.Equal("email_conflict", duplicateEmail!.Message.Code);

                var late = request with { OperationId = Guid.NewGuid(), Email = "late@example.com" };
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
                Assert.True(retry.Is(out Response<UserRegistered>? _));
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


