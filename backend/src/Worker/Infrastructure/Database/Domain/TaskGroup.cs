using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kaban.Worker.Infrastructure.Database.Domain;

public enum PriorityEnum
{
    [Display(Name = "No Priority")]
    NoPriority,
    [Display(Name = "Low")]
    Low,
    [Display(Name = "Medium")]
    Medium,
    [Display(Name = "High")]
    High,
    [Display(Name = "Very high")]
    VeryHigh
}

public class Tasks : ITable
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public PriorityEnum Priority { get; set; } = PriorityEnum.NoPriority;
    public required DateOnly Created { get; set; }
    public DateOnly? Deadline { get; set; }
    public required int TaskOrder { get; set; }

    public int BoardColumnId { get; set; }
    public BoardColumns? BoardColumn { get; set; }
}

public sealed class TasksConfiguration : IEntityTypeConfiguration<Tasks>
{
    public void Configure(EntityTypeBuilder<Tasks> builder)
    {
        builder.ToTable(TableExtensions.GetDatabaseName(nameof(Tasks)));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName(TableExtensions.GetDatabaseName(nameof(Tasks.Id))).ValueGeneratedOnAdd();
        builder.Property(x => x.Name)
            .HasColumnName(TableExtensions.GetDatabaseName(nameof(Tasks.Name))).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Description)
            .HasColumnName(TableExtensions.GetDatabaseName(nameof(Tasks.Description)));
        builder.Property(x => x.Priority)
            .HasColumnName(TableExtensions.GetDatabaseName(nameof(Tasks.Priority)))
            .HasColumnType("task_priority")
            .HasDefaultValue(PriorityEnum.NoPriority)
            .IsRequired();
        builder.Property(x => x.Created)
            .HasColumnName("task_created").IsRequired();
        builder.Property(x => x.Deadline)
            .HasColumnName(TableExtensions.GetDatabaseName(nameof(Tasks.Deadline)));
        builder.Property(x => x.TaskOrder)
            .HasColumnName(TableExtensions.GetDatabaseName(nameof(Tasks.TaskOrder)));
        builder.Property(x => x.BoardColumnId)
            .HasColumnName(TableExtensions.GetDatabaseName(nameof(Tasks.BoardColumnId)));
        builder.HasIndex(x => x.BoardColumnId);
        builder.HasIndex(x => new { x.BoardColumnId, x.TaskOrder }).IsUnique();
        builder.HasOne(x => x.BoardColumn).WithMany().HasForeignKey(x => x.BoardColumnId);
    }
}

public class TaskTags : ITable
{
    public int Id { get; set; }
    public required string TagName { get; set; }

    public int TaskId { get; set; }
    public Tasks? Task { get; set; }
}

public sealed class TaskTagsConfiguration : IEntityTypeConfiguration<TaskTags>
{
    public void Configure(EntityTypeBuilder<TaskTags> builder)
    {
        builder.ToTable(TableExtensions.GetDatabaseName(nameof(TaskTags)));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName(TableExtensions.GetDatabaseName(nameof(TaskTags.Id))).ValueGeneratedOnAdd();
        builder.Property(x => x.TagName)
            .HasColumnName("tag").HasMaxLength(32).IsRequired();
        builder.Property(x => x.TaskId)
            .HasColumnName(TableExtensions.GetDatabaseName(nameof(TaskTags.TaskId)));
        builder.HasIndex(x => x.TaskId);
        builder.HasIndex(x => new { x.TaskId, x.TagName }).IsUnique();
        builder.HasOne(x => x.Task).WithMany().HasForeignKey(x => x.TaskId);
    }
}

public class UserTasks : ITable
{
    public int UserId { get; set; }
    public Users? User { get; set; }

    public int TaskId { get; set; }
    public Tasks? Task { get; set; }
}

public sealed class UserTasksConfiguration : IEntityTypeConfiguration<UserTasks>
{
    public void Configure(EntityTypeBuilder<UserTasks> builder)
    {
        builder.ToTable(TableExtensions.GetDatabaseName(nameof(UserTasks)));
        builder.HasKey(x => new { x.UserId, x.TaskId });
        builder.Property(x => x.UserId)
            .HasColumnName(TableExtensions.GetDatabaseName(nameof(UserTasks.UserId)));
        builder.Property(x => x.TaskId)
            .HasColumnName(TableExtensions.GetDatabaseName(nameof(UserTasks.TaskId)));
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.TaskId);
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        builder.HasOne(x => x.Task).WithMany().HasForeignKey(x => x.TaskId);
    }
}
