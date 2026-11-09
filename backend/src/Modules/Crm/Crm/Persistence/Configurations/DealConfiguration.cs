using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NpgsqlTypes;
using Parley.Crm.Domain.Deals;
using Parley.Crm.Domain.Pipelines;
using Parley.Crm.Shared;
using Parley.SharedKernel.Persistence.Conventions;

namespace Parley.Crm.Persistence.Configurations;

/// <summary>Mapping of <see cref="Deal"/>: board and list indexes that start with the organization, search and JSON indexes.</summary>
public sealed class DealConfiguration : IEntityTypeConfiguration<Deal>
{
    public void Configure(EntityTypeBuilder<Deal> builder)
    {
        builder.ToTable(CrmSchemaNames.DealsTable, table =>
            table.HasCheckConstraint(CrmSchemaNames.DealStatusCheck, CrmSql.DealStatusCheck));
        builder.Property(deal => deal.Title).HasMaxLength(CrmLimits.DealTitleMaximumLength).IsRequired();
        builder.Property(deal => deal.TitleNormalized)
            .HasComputedColumnSql(CrmSql.DealTitleNormalized, stored: true);
        builder.Property(deal => deal.Amount).HasPrecision(CrmLimits.MoneyPrecision, CrmLimits.MoneyScale);
        builder.Property(deal => deal.Currency).HasColumnType("char(3)").IsRequired();
        builder.Property(deal => deal.Status).HasMaxLength(CrmLimits.SourceMaximumLength).IsRequired();
        builder.Property(deal => deal.LostReason).HasMaxLength(CrmLimits.DealLostReasonMaximumLength);
        builder.Property(deal => deal.ClosedAt).HasColumnType("timestamptz").HasConversion(UtcValueConverters.Optional);
        builder.Property(deal => deal.DeletedAt).HasColumnType("timestamptz").HasConversion(UtcValueConverters.Optional);
        builder.Property(deal => deal.CustomFields).HasColumnType("jsonb").IsRequired();
        builder.Property(deal => deal.Source).HasMaxLength(CrmLimits.SourceMaximumLength).IsRequired();
        builder.Property<NpgsqlTsVector>(CrmSchemaNames.SearchVectorProperty)
            .HasColumnName(CrmSchemaNames.SearchVectorColumn)
            .HasComputedColumnSql(CrmSql.DealSearchVector, stored: true);

        builder.HasOne<Pipeline>()
            .WithMany()
            .HasForeignKey(deal => deal.PipelineId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PipelineStage>()
            .WithMany()
            .HasForeignKey(deal => deal.StageId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasTenantIndex(CrmSchemaNames.DealBoardIndex, nameof(Deal.PipelineId), nameof(Deal.StageId), nameof(Deal.BoardPosition));
        builder.HasTenantIndex(CrmSchemaNames.DealOwnerStatusIndex, nameof(Deal.OwnerUserId), nameof(Deal.Status));
        builder.HasTenantIndex(CrmSchemaNames.DealNameIndex, nameof(Deal.TitleNormalized), nameof(Deal.Id));
        builder.HasTenantIndex(CrmSchemaNames.DealAmountIndex, nameof(Deal.Amount), nameof(Deal.Id));
        builder.HasTenantIndex(CrmSchemaNames.DealCreatedIndex, nameof(Deal.CreatedAt), nameof(Deal.Id));
        builder.HasIndex(CrmSchemaNames.SearchVectorProperty)
            .HasDatabaseName(CrmSchemaNames.DealSearchIndex)
            .HasMethod(CrmSchemaNames.GinMethod);
        builder.HasIndex(deal => deal.CustomFields)
            .HasDatabaseName(CrmSchemaNames.DealCustomFieldsIndex)
            .HasMethod(CrmSchemaNames.GinMethod)
            .HasOperators(CrmSchemaNames.JsonPathOperatorClass);
    }
}
