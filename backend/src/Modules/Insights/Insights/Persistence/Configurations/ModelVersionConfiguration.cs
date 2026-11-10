using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Parley.Insights.Domain.Ml;
using Parley.Insights.Shared;

namespace Parley.Insights.Persistence.Configurations;

/// <summary>Mapping of <see cref="ModelVersion"/>: a platform table, unique on model name and version.</summary>
public sealed class ModelVersionConfiguration : IEntityTypeConfiguration<ModelVersion>
{
    public void Configure(EntityTypeBuilder<ModelVersion> builder)
    {
        builder.ToTable(InsightsSchemaNames.ModelVersionsTable);
        builder.Property(version => version.ModelName).HasMaxLength(InsightsLimits.VersionMaximumLength).IsRequired();
        builder.Property(version => version.Version).HasMaxLength(InsightsLimits.VersionMaximumLength).IsRequired();
        builder.Property(version => version.Kind).HasMaxLength(InsightsLimits.CodeMaximumLength).IsRequired();
        builder.Property(version => version.Provenance).HasMaxLength(InsightsLimits.CodeMaximumLength).IsRequired();
        builder.Property(version => version.TrainedAt).HasColumnType("timestamptz");
        builder.Property(version => version.SyncedAt).HasColumnType("timestamptz");
        builder.Property(version => version.Metrics).HasColumnType("jsonb");
        builder.HasIndex(nameof(ModelVersion.ModelName), nameof(ModelVersion.Version))
            .IsUnique()
            .HasDatabaseName(InsightsSchemaNames.ModelVersionNameIndex);
    }
}
