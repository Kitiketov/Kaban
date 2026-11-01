using Database;
using Database.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kaban.Worker.Infrastructure.Database.Domain;

public class Teams : ITable
{
    public int Id { get; set; }
    public required string TeamName { get; set; }
}

public sealed class TeamsConfiguration : IEntityTypeConfiguration<Teams>
{
    public void Configure(EntityTypeBuilder<Teams> builder)
    {
        builder.ToTable(TableExtensions.GetDatabaseName(nameof(Teams)));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName(TableExtensions.GetDatabaseName(nameof(Teams.Id))).ValueGeneratedOnAdd();
        builder.Property(x => x.TeamName)
            .HasColumnName(TableExtensions.GetDatabaseName(nameof(Teams.TeamName))).IsRequired().HasMaxLength(64);
    }
}

public class Projects : ITable
{
    public int Id { get; set; }
    public required string ProjectName { get; set; }
}

public sealed class ProjectsConfiguration : IEntityTypeConfiguration<Projects>
{
    public void Configure(EntityTypeBuilder<Projects> builder)
    {
        builder.ToTable(TableExtensions.GetDatabaseName(nameof(Projects)));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName(TableExtensions.GetDatabaseName(nameof(Projects.Id))).ValueGeneratedOnAdd();
        builder.Property(x => x.ProjectName)
            .HasColumnName(TableExtensions.GetDatabaseName(nameof(Projects.ProjectName))).IsRequired().HasMaxLength(64);
    }
}

public class Roles : ITable
{
    public int Id { get; set; }
    public required string RoleName { get; set; }

    public int TeamId { get; set; }
    public Teams? Team { get; set; }
}

public sealed class RolesConfiguration : IEntityTypeConfiguration<Roles>
{
    public void Configure(EntityTypeBuilder<Roles> builder)
    {
        builder.ToTable(TableExtensions.GetDatabaseName(nameof(Roles)));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName(TableExtensions.GetDatabaseName(nameof(Roles.Id))).ValueGeneratedOnAdd();
        builder.Property(x => x.RoleName)
            .HasColumnName(TableExtensions.GetDatabaseName(nameof(Roles.RoleName))).IsRequired().HasMaxLength(64);
        builder.Property(x => x.TeamId)
            .HasColumnName(TableExtensions.GetDatabaseName(nameof(Roles.TeamId)));
        builder.HasIndex(x => x.TeamId);
        builder.HasIndex(x => new { x.TeamId, x.RoleName }).IsUnique();
        builder.HasOne(x => x.Team).WithMany().HasForeignKey(x => x.TeamId);
    }
}

public class TeamsProjects : ITable
{
    public int ProjectId { get; set; }
    public Projects? Project { get; set; }

    public int TeamId { get; set; }
    public Teams? Team { get; set; }
}

public sealed class TeamsProjectsConfiguration : IEntityTypeConfiguration<TeamsProjects>
{
    public void Configure(EntityTypeBuilder<TeamsProjects> builder)
    {
        builder.ToTable(TableExtensions.GetDatabaseName(nameof(TeamsProjects)));
        builder.HasKey(x => new { x.ProjectId, x.TeamId });
        builder.Property(x => x.ProjectId)
            .HasColumnName(TableExtensions.GetDatabaseName(nameof(TeamsProjects.ProjectId)));
        builder.Property(x => x.TeamId)
            .HasColumnName(TableExtensions.GetDatabaseName(nameof(TeamsProjects.TeamId)));
        builder.HasIndex(x => x.ProjectId);
        builder.HasIndex(x => x.TeamId);
        builder.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId);
        builder.HasOne(x => x.Team).WithMany().HasForeignKey(x => x.TeamId);
    }
}
