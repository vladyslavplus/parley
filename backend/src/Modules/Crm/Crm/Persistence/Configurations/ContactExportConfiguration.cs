using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Parley.Crm.Domain.Exports;
using Parley.Crm.Shared;
using Parley.SharedKernel.Persistence.Conventions;

namespace Parley.Crm.Persistence.Configurations;

public sealed class ContactExportConfiguration : IEntityTypeConfiguration<ContactExport>
{
    public void Configure(EntityTypeBuilder<ContactExport> builder)
    {
        builder.ToTable(CrmSchemaNames.ContactExportsTable);
        builder.Property(export => export.Status).HasMaxLength(CrmLimits.ExportStatusMaximumLength).IsRequired();
        builder.Property(export => export.FilterJson).HasColumnType("jsonb").IsRequired();
        builder.Property(export => export.FileName).HasMaxLength(CrmLimits.ExportFileNameMaximumLength);
        builder.Property(export => export.CompletedAt).HasColumnType("timestamptz");
        builder.Property(export => export.ExpiresAt).HasColumnType("timestamptz");
        builder.HasTenantIndex(
            CrmSchemaNames.ContactExportRequesterIndex,
            nameof(ContactExport.RequestedByUserId),
            nameof(ContactExport.CreatedAt));
        builder.HasIndex(export => export.ExpiresAt).HasDatabaseName(CrmSchemaNames.ContactExportExpiresIndex);
    }
}
