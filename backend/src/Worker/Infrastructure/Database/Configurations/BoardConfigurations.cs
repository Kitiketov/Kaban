using Kaban.Worker.Features.Boards.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kaban.Worker.Infrastructure.Database.Configurations;

public sealed class BoardsConfiguration : IEntityTypeConfiguration<Boards>
{
    public void Configure(EntityTypeBuilder<Boards> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Name).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => x.ProjectId);
        builder.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId);
    }
}

public sealed class BoardColumnsConfiguration : IEntityTypeConfiguration<BoardColumns>
{
    public void Configure(EntityTypeBuilder<BoardColumns> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.ColumnName).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => x.BoardId);
        builder.HasIndex(x => new { x.BoardId, x.BoardOrder }).IsUnique();
        builder.HasOne(x => x.Board).WithMany().HasForeignKey(x => x.BoardId);
    }
}

public sealed class BoardTagsConfiguration : IEntityTypeConfiguration<BoardTags>
{
    public void Configure(EntityTypeBuilder<BoardTags> builder)
    {
        builder.HasKey(x => new { x.TagId, x.BoardId });
        builder.HasIndex(x => x.BoardId);
        builder.HasIndex(x => x.TagId);
        builder.HasOne(x => x.Board).WithMany().HasForeignKey(x => x.BoardId);
        builder.HasOne(x => x.Tag).WithMany().HasForeignKey(x => x.TagId);
    }
}
