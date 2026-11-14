using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Migrations.Internal;

namespace Kaban.Worker.Infrastructure.Database;

#pragma warning disable EF1001
public sealed class KabanHistoryRepository(HistoryRepositoryDependencies dependencies)
    : NpgsqlHistoryRepository(dependencies)
{
    protected override string MigrationIdColumnName => "MigrationId";
    protected override string ProductVersionColumnName => "ProductVersion";
}
#pragma warning restore EF1001
