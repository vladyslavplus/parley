using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Parley.Crm.Domain.CustomFields;
using Parley.Crm.Shared;
using Parley.SharedKernel.Persistence.Conventions;

namespace Parley.Crm.Persistence.Configurations;

/// <summary>Mapping of <see cref="CustomFieldDefinition"/>: keys unique per organization and record type.</summary>
public sealed class CustomFieldDefinitionConfiguration : IEntityTypeConfiguration<CustomFieldDefinition>
{
    public void Configure(EntityTypeBuilder<CustomFieldDefinition> builder)
    {
        builder.ToTable(CrmSchemaNames.CustomFieldDefinitionsTable);
        builder.Property(definition => definition.EntityType).HasMaxLength(CrmLimits.CustomFieldEntityTypeMaximumLength).IsRequired();
        builder.Property(definition => definition.Key).HasMaxLength(CrmLimits.CustomFieldKeyMaximumLength).IsRequired();
        builder.Property(definition => definition.Label).HasMaxLength(CrmLimits.CustomFieldLabelMaximumLength).IsRequired();
        builder.Property(definition => definition.FieldType).HasMaxLength(CrmLimits.CustomFieldFieldTypeMaximumLength).IsRequired();
        builder.Property(definition => definition.Options).HasColumnType("jsonb").IsRequired();
        builder.Property(definition => definition.ArchivedAt).HasColumnType("timestamptz");
        builder.HasTenantUniqueIndex(
            CrmSchemaNames.CustomFieldKeyIndex,
            nameof(CustomFieldDefinition.EntityType),
            nameof(CustomFieldDefinition.Key));
    }
}
