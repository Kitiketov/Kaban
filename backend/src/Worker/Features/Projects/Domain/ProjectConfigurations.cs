using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kaban.Worker.Features.Projects.Domain;

public sealed class ProjectsConfiguration : IEntityTypeConfiguration<Projects>
{
    public void Configure(EntityTypeBuilder<Projects> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.ProjectName).IsRequired().HasMaxLength(64);
    }
}

public sealed class TeamsProjectsConfiguration : IEntityTypeConfiguration<TeamProjects>
{
    public void Configure(EntityTypeBuilder<TeamProjects> builder)
    {
        builder.HasKey(x => new { x.ProjectId, x.TeamId });
        builder.HasIndex(x => x.ProjectId);
        builder.HasIndex(x => x.TeamId);
        builder.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId);
        builder.HasOne(x => x.Team).WithMany().HasForeignKey(x => x.TeamId);
    }
}
