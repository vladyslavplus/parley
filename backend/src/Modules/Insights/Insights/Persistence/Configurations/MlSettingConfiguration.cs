using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Parley.Insights.Domain.Ml;
using Parley.Insights.Shared;

namespace Parley.Insights.Persistence.Configurations;

/// <summary>Mapping of <see cref="MlSetting"/>: one row per organization.</summary>
public sealed class MlSettingConfiguration : IEntityTypeConfiguration<MlSetting>
{
    public void Configure(EntityTypeBuilder<MlSetting> builder)
    {
        builder.ToTable(InsightsSchemaNames.MlSettingsTable);
        builder.Property(setting => setting.ActiveUpliftVersion).HasMaxLength(InsightsLimits.VersionMaximumLength);
        builder.Property(setting => setting.ActiveClvVersion).HasMaxLength(InsightsLimits.VersionMaximumLength);
        builder.HasIndex(nameof(MlSetting.OrganizationId))
            .IsUnique()
            .HasDatabaseName(InsightsSchemaNames.MlSettingOrganizationIndex);
    }
}
