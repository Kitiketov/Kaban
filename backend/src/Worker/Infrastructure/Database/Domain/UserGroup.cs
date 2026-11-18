using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kaban.Worker.Infrastructure.Database.Domain;

public class Users : ITable
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required string HashedPassword { get; set; }
}

public sealed class UsersConfiguration : IEntityTypeConfiguration<Users>
{
    public void Configure(EntityTypeBuilder<Users> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Email).IsUnique();
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Name).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(320).IsRequired();
        builder.Property(x => x.HashedPassword)
            .HasColumnName("hash_password").HasMaxLength(255).IsRequired();
    }
}

public class UserTeams : ITable
{
    public int UserId { get; set; }
    public Users? User { get; set; }
    public int TeamId { get; set; }
    public Teams? Team { get; set; }
    public int RoleId { get; set; }
    public Roles? Role { get; set; }
}

public sealed class UsersTeamsConfiguration : IEntityTypeConfiguration<UserTeams>
{
    public void Configure(EntityTypeBuilder<UserTeams> builder)
    {
        builder.HasKey(x => new { x.UserId, x.TeamId });
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.TeamId);
        builder.HasIndex(x => x.RoleId);
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        builder.HasOne(x => x.Team).WithMany().HasForeignKey(x => x.TeamId);
        builder.HasOne(x => x.Role).WithMany().HasForeignKey(x => x.RoleId);
    }
}