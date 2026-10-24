using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Database.Domain;

public class Boards : ITable
{
    public int Id { get; set; }
    public required string Name { get; set; }

    public int ProjectId { get; set; }
    public Projects? Project { get; set; }
}

public sealed class BoardsConfiguration : IEntityTypeConfiguration<Boards>
{
    public void Configure(EntityTypeBuilder<Boards> builder)
    {
        builder.ToTable(TableExtensions.GetDatabaseName(nameof(Boards)));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName(TableExtensions.GetDatabaseName(nameof(Boards.Id))).ValueGeneratedOnAdd();
        builder.Property(x => x.Name)
            .HasColumnName(TableExtensions.GetDatabaseName(nameof(Boards.Name))).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ProjectId)
            .HasColumnName(TableExtensions.GetDatabaseName(nameof(Boards.ProjectId)));
        builder.HasIndex(x => x.ProjectId);
        builder.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId);
    }
}

public class BoardColumns : ITable
{
    public int Id { get; set; }
    public required string ColumnName { get; set; }
    public int BoardOrder { get; set; }

    public int BoardId { get; set; }
    public Boards? Board { get; set; }
}

public sealed class BoardColumnsConfiguration : IEntityTypeConfiguration<BoardColumns>
{
    public void Configure(EntityTypeBuilder<BoardColumns> builder)
    {
        builder.ToTable(TableExtensions.GetDatabaseName(nameof(BoardColumns)));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName(TableExtensions.GetDatabaseName(nameof(BoardColumns.Id))).ValueGeneratedOnAdd();
        builder.Property(x => x.ColumnName)
            .HasColumnName(TableExtensions.GetDatabaseName(nameof(BoardColumns.ColumnName))).HasMaxLength(64).IsRequired();
        builder.Property(x => x.BoardOrder)
            .HasColumnName(TableExtensions.GetDatabaseName(nameof(BoardColumns.BoardOrder)));
        builder.Property(x => x.BoardId)
            .HasColumnName(TableExtensions.GetDatabaseName(nameof(BoardColumns.BoardId)));
        builder.HasIndex(x => x.BoardId);
        builder.HasIndex(x => new { x.BoardId, x.BoardOrder }).IsUnique();
        builder.HasOne(x => x.Board).WithMany().HasForeignKey(x => x.BoardId);
    }
}
