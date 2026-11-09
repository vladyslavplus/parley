using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Parley.Crm.Domain.Contacts;
using Parley.Crm.Domain.Orders;
using Parley.Crm.Shared;
using Parley.SharedKernel.Persistence.Conventions;

namespace Parley.Crm.Persistence.Configurations;

/// <summary>Mapping of <see cref="Order"/>: contact history reads and replay-safe source keys.</summary>
public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable(CrmSchemaNames.OrdersTable);
        builder.Property(order => order.ExternalRef).HasMaxLength(CrmLimits.ExternalRefMaximumLength).IsRequired();
        builder.Property(order => order.OrderedAt).HasColumnType("timestamptz").HasConversion(UtcValueConverters.Required);
        builder.Property(order => order.TotalAmount).HasPrecision(CrmLimits.MoneyPrecision, CrmLimits.MoneyScale);
        builder.Property(order => order.Currency).HasColumnType("char(3)").IsRequired();
        builder.Property(order => order.Source).HasMaxLength(CrmLimits.SourceMaximumLength).IsRequired();
        builder.HasOne<Contact>()
            .WithMany()
            .HasForeignKey(order => order.ContactId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasTenantIndex(CrmSchemaNames.OrderContactIndex, nameof(Order.ContactId), nameof(Order.OrderedAt));
        builder.HasTenantUniqueIndex(CrmSchemaNames.OrderSourceIndex, nameof(Order.Source), nameof(Order.ExternalRef));
    }
}
