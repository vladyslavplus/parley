using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Parley.Crm.Domain.Activities;
using Parley.Crm.Shared;
using Parley.SharedKernel.Persistence.Conventions;

namespace Parley.Crm.Persistence.Configurations;

/// <summary>Mapping of <see cref="Activity"/>: timeline reads per contact, replay-safe source events.</summary>
public sealed class ActivityConfiguration : IEntityTypeConfiguration<Activity>
{
    public void Configure(EntityTypeBuilder<Activity> builder)
    {
        builder.ToTable(CrmSchemaNames.ActivitiesTable);
        builder.Property(activity => activity.Type).HasMaxLength(CrmLimits.SourceMaximumLength).IsRequired();
        builder.Property(activity => activity.Payload).HasColumnType("jsonb").IsRequired();
        builder.Property(activity => activity.OccurredAt).HasColumnType("timestamptz");
        builder.HasTenantIndex(CrmSchemaNames.ActivityContactIndex, nameof(Activity.ContactId), nameof(Activity.OccurredAt));
        builder.HasTenantUniqueIndex(CrmSchemaNames.ActivitySourceEventIndex, nameof(Activity.SourceEventId));
    }
}
