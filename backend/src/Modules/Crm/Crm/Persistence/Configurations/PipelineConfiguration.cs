using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Parley.Crm.Domain.Pipelines;
using Parley.Crm.Shared;
using Parley.SharedKernel.Persistence.Conventions;

namespace Parley.Crm.Persistence.Configurations;

/// <summary>Mapping of <see cref="Pipeline"/>: one default pipeline per organization, stages cascade with it.</summary>
public sealed class PipelineConfiguration : IEntityTypeConfiguration<Pipeline>
{
    public void Configure(EntityTypeBuilder<Pipeline> builder)
    {
        builder.ToTable(CrmSchemaNames.PipelinesTable);
        builder.Property(pipeline => pipeline.Name).HasMaxLength(CrmLimits.PipelineNameMaximumLength).IsRequired();
        builder.Property(pipeline => pipeline.SystemKey).HasMaxLength(CrmLimits.PipelineSystemKeyMaximumLength);
        builder.HasTenantUniqueIndex(CrmSchemaNames.PipelineDefaultIndex)
            .HasFilter(CrmSql.DefaultPipelineFilter);
        builder.HasMany(pipeline => pipeline.Stages)
            .WithOne()
            .HasForeignKey(stage => stage.PipelineId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
