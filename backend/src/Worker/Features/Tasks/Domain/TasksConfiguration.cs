using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kaban.Worker.Features.Tasks.Domain;

public sealed class TasksConfiguration : IEntityTypeConfiguration<Tasks>
{
    public void Configure(EntityTypeBuilder<Tasks> builder)
    {
        builder.ToTable(table => table.HasCheckConstraint(
                "ck_tasks_priority",
                "\"priority\" IN ('NoPriority', 'Low', 'Medium', 'High', 'VeryHigh')"
            ));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Name).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Priority)
            .HasConversion<string>()
            .HasMaxLength(16)
            .HasDefaultValueSql("'NoPriority'")
            .IsRequired();
        builder.Property(x => x.Created)
            .HasColumnName("task_created")
            .HasDefaultValueSql("now()")
            .IsRequired();
        builder.Property(x => x.TaskRank)
            .HasMaxLength(128)
            .UseCollation("C")
            .IsRequired();
        builder.HasIndex(x => x.BoardColumnId);
        builder.HasIndex(x => new { x.BoardColumnId, x.TaskRank }).IsUnique();
        builder.HasOne(x => x.BoardColumn).WithMany(x => x.Tasks).HasForeignKey(x => x.BoardColumnId);
    }
}

public sealed class TaskTagsConfiguration : IEntityTypeConfiguration<TaskTags>
{
    public void Configure(EntityTypeBuilder<TaskTags> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.TagName)
            .HasColumnName("tag").HasMaxLength(32).IsRequired();
        builder.HasIndex(x => x.TaskId);
        builder.HasIndex(x => new { x.TaskId, x.TagName }).IsUnique();
        builder.HasOne(x => x.Task).WithMany().HasForeignKey(x => x.TaskId);
    }
}

public sealed class UserTasksConfiguration : IEntityTypeConfiguration<UserTasks>
{
    public void Configure(EntityTypeBuilder<UserTasks> builder)
    {
        builder.HasKey(x => new { x.UserId, x.TaskId });
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.TaskId);
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        builder.HasOne(x => x.Task).WithMany().HasForeignKey(x => x.TaskId);
    }
}
