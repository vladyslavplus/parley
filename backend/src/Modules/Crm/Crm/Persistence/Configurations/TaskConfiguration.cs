using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Parley.Crm.Domain.Tasks;
using Parley.Crm.Shared;
using Parley.SharedKernel.Persistence.Conventions;

namespace Parley.Crm.Persistence.Configurations;

/// <summary>Mapping of <see cref="CrmTask"/>: assignee and due-date reads, title search and the open-task sweep index.</summary>
public sealed class TaskConfiguration : IEntityTypeConfiguration<CrmTask>
{
    public void Configure(EntityTypeBuilder<CrmTask> builder)
    {
        builder.ToTable(CrmSchemaNames.TasksTable);
        builder.Property(task => task.Title).HasMaxLength(CrmLimits.TaskTitleMaximumLength).IsRequired();
        builder.Property(task => task.TitleNormalized).HasComputedColumnSql(CrmSql.TaskTitleNormalized, stored: true);
        builder.Property(task => task.Notes).HasMaxLength(CrmLimits.TaskNotesMaximumLength);
        builder.Property(task => task.DueAt).HasColumnType("timestamptz").HasConversion(UtcValueConverters.Optional);
        builder.Property(task => task.CompletedAt).HasColumnType("timestamptz").HasConversion(UtcValueConverters.Optional);
        builder.Property(task => task.ReminderSentAt).HasColumnType("timestamptz").HasConversion(UtcValueConverters.Optional);
        builder.Property(task => task.DeletedAt).HasColumnType("timestamptz").HasConversion(UtcValueConverters.Optional);

        builder.HasTenantIndex(CrmSchemaNames.TaskAssigneeIndex, nameof(CrmTask.AssigneeUserId), nameof(CrmTask.DueAt), nameof(CrmTask.Id));
        builder.HasTenantIndex(CrmSchemaNames.TaskCreatedIndex, nameof(CrmTask.CreatedAt), nameof(CrmTask.Id));
        builder.HasTenantIndex(CrmSchemaNames.TaskTitleIndex, nameof(CrmTask.TitleNormalized), nameof(CrmTask.Id));
        builder.HasIndex(task => new { task.DueAt, task.Id })
            .HasDatabaseName(CrmSchemaNames.TaskReminderIndex)
            .HasFilter(CrmSql.OpenUnremindedTaskFilter);
    }
}
