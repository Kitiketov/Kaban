using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kaban.Worker.Features.Teams.Domain;

public sealed class TeamsConfiguration : IEntityTypeConfiguration<Teams>
{
    public void Configure(EntityTypeBuilder<Teams> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.TeamName).IsRequired().HasMaxLength(64);
    }
}

public sealed class RolesConfiguration : IEntityTypeConfiguration<Roles>
{
    public void Configure(EntityTypeBuilder<Roles> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.RoleName).IsRequired().HasMaxLength(64);
        builder.HasIndex(x => x.TeamId);
        builder.HasIndex(x => new { x.TeamId, x.RoleName }).IsUnique();
        builder.HasOne(x => x.Team).WithMany().HasForeignKey(x => x.TeamId);
    }
}
