using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Parley.Crm.Domain.Pipelines;
using Parley.Crm.Shared;
using Parley.SharedKernel.Persistence.Conventions;

namespace Parley.Crm.Persistence.Configurations;

/// <summary>Mapping of <see cref="PipelineStage"/>: unique names and positions per pipeline, checked kinds and ranges.</summary>
public sealed class PipelineStageConfiguration : IEntityTypeConfiguration<PipelineStage>
{
    public void Configure(EntityTypeBuilder<PipelineStage> builder)
    {
        builder.ToTable(CrmSchemaNames.PipelineStagesTable, table =>
        {
            table.HasCheckConstraint(CrmSchemaNames.PipelineStageKindCheck, CrmSql.StageKindCheck);
            table.HasCheckConstraint(CrmSchemaNames.PipelineStageColorCheck, CrmSql.StageColorCheck);
            table.HasCheckConstraint(CrmSchemaNames.PipelineStageProbabilityCheck, CrmSql.StageProbabilityCheck);
        });
        builder.Property(stage => stage.Name).HasMaxLength(CrmLimits.PipelineStageNameMaximumLength).IsRequired();
        builder.Property(stage => stage.SystemKey).HasMaxLength(CrmLimits.PipelineSystemKeyMaximumLength);
        builder.Property(stage => stage.NameNormalized)
            .HasComputedColumnSql(CrmSql.StageNameNormalized, stored: true);
        builder.Property(stage => stage.Kind).HasMaxLength(CrmLimits.SourceMaximumLength).IsRequired();
        builder.Property(stage => stage.ColorIndex).HasColumnType("smallint");

        builder.HasTenantUniqueIndex(CrmSchemaNames.PipelineStageNameIndex, nameof(PipelineStage.PipelineId), nameof(PipelineStage.NameNormalized));
        builder.HasTenantUniqueIndex(CrmSchemaNames.PipelineStagePositionIndex, nameof(PipelineStage.PipelineId), nameof(PipelineStage.Position));
    }
}
