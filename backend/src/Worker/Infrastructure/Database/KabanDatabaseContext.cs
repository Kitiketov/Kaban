using Kaban.Worker.Infrastructure.Database.Domain;
using Microsoft.EntityFrameworkCore;

namespace Kaban.Worker.Infrastructure.Database;

public class KabanDatabaseContext(DbContextOptions<KabanDatabaseContext> options) : DbContext(options)
{
    public DbSet<Tasks> Tasks => Set<Tasks>();
    public DbSet<Projects> Projects => Set<Projects>();
    public DbSet<BoardColumns> BoardColumns => Set<BoardColumns>();
    public DbSet<Boards> Boards => Set<Boards>();
    public DbSet<UserTasks> UserTasks => Set<UserTasks>();
    public DbSet<Users> Users => Set<Users>();
    public DbSet<TeamProjects> TeamProjects => Set<TeamProjects>();
    public DbSet<UserTeams> UserTeams => Set<UserTeams>();
    public DbSet<Teams> Teams => Set<Teams>();
    public DbSet<Roles> Roles => Set<Roles>();
    public DbSet<TaskTags> TaskTags => Set<TaskTags>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasPostgresEnum<PriorityEnum>("task_priority");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(KabanDatabaseContext).Assembly);
    }
}