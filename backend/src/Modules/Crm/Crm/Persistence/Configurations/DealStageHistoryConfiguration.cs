using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Parley.Crm.Domain.Deals;
using Parley.SharedKernel.Persistence.Conventions;

namespace Parley.Crm.Persistence.Configurations;

/// <summary>Mapping of <see cref="DealStageHistoryEntry"/>: append-only, read per deal in time order.</summary>
public sealed class DealStageHistoryConfiguration : IEntityTypeConfiguration<DealStageHistoryEntry>
{
    public void Configure(EntityTypeBuilder<DealStageHistoryEntry> builder)
    {
        builder.ToTable(CrmSchemaNames.DealStageHistoryTable);
        builder.Property(entry => entry.ChangedAt).HasColumnType("timestamptz").HasConversion(UtcValueConverters.Required);
        builder.HasTenantIndex(CrmSchemaNames.DealStageHistoryDealIndex, nameof(DealStageHistoryEntry.DealId), nameof(DealStageHistoryEntry.ChangedAt));
    }
}
