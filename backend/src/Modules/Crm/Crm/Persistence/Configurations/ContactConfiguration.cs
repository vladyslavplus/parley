using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NpgsqlTypes;
using Parley.Crm.Domain.Contacts;
using Parley.Crm.Shared;
using Parley.SharedKernel.Persistence.Conventions;

namespace Parley.Crm.Persistence.Configurations;

/// <summary>Mapping of <see cref="Contact"/>: live-row unique email, keyset and search indexes.</summary>
public sealed class ContactConfiguration : IEntityTypeConfiguration<Contact>
{
    private static readonly ValueConverter<DateTime?, DateTime?> NullableUtcConverter = new(
        value => value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : value,
        value => value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : value);

    public void Configure(EntityTypeBuilder<Contact> builder)
    {
        builder.ToTable(CrmSchemaNames.ContactsTable);
        builder.Property(contact => contact.FirstName).HasMaxLength(CrmLimits.PersonNameMaximumLength).IsRequired();
        builder.Property(contact => contact.LastName).HasMaxLength(CrmLimits.PersonNameMaximumLength);
        builder.Property(contact => contact.Email).HasMaxLength(CrmLimits.EmailMaximumLength);
        builder.Property(contact => contact.EmailNormalized).HasMaxLength(CrmLimits.EmailMaximumLength);
        builder.Property(contact => contact.Phone).HasMaxLength(CrmLimits.PhoneMaximumLength);
        builder.Property(contact => contact.JobTitle).HasMaxLength(CrmLimits.JobTitleMaximumLength);
        builder.Property(contact => contact.Country).HasColumnType("char(2)");
        builder.Property(contact => contact.City).HasMaxLength(CrmLimits.CityMaximumLength);
        builder.Property(contact => contact.Source).HasMaxLength(CrmLimits.SourceMaximumLength).IsRequired();
        builder.Property(contact => contact.CustomFields).HasColumnType("jsonb").IsRequired();
        builder.Property(contact => contact.DeletedAt).HasColumnType("timestamptz").HasConversion(NullableUtcConverter);
        builder.Property(contact => contact.ExternalRef).HasMaxLength(CrmLimits.ExternalRefMaximumLength);
        builder.Property(contact => contact.LastOrderAt).HasColumnType("timestamptz").HasConversion(UtcValueConverters.Optional);
        builder.Property(contact => contact.NameNormalized)
            .HasComputedColumnSql(CrmSql.ContactNameNormalized, stored: true);
        builder.Property<NpgsqlTsVector>(CrmSchemaNames.SearchVectorProperty)
            .HasColumnName(CrmSchemaNames.SearchVectorColumn)
            .HasComputedColumnSql(CrmSql.ContactSearchVector, stored: true);


        builder.HasTenantIndex(CrmSchemaNames.ContactIdIndex, nameof(Contact.Id));
        builder.HasTenantIndex(CrmSchemaNames.ContactOwnerIndex, nameof(Contact.OwnerUserId), nameof(Contact.Id));
        builder.HasTenantIndex(CrmSchemaNames.ContactCreatedIndex, nameof(Contact.CreatedAt), nameof(Contact.Id));
        builder.HasTenantIndex(CrmSchemaNames.ContactNameIndex, nameof(Contact.NameNormalized), nameof(Contact.Id));
        builder.HasTenantIndex(CrmSchemaNames.ContactCompanyIndex, nameof(Contact.CompanyId), nameof(Contact.Id));
        builder.HasTenantUniqueIndex(CrmSchemaNames.ContactEmailIndex, nameof(Contact.EmailNormalized))
            .HasFilter(CrmSql.LiveRowFilter);
        builder.HasTenantUniqueIndex(CrmSchemaNames.ContactSourceExternalRefIndex, nameof(Contact.Source), nameof(Contact.ExternalRef))
            .HasFilter(CrmSql.ExternalRefPresentFilter);
        builder.HasIndex(CrmSchemaNames.SearchVectorProperty)
            .HasDatabaseName(CrmSchemaNames.ContactSearchIndex)
            .HasMethod(CrmSchemaNames.GinMethod);
        builder.HasIndex(contact => contact.NameNormalized)
            .HasDatabaseName(CrmSchemaNames.ContactNameTrigramIndex)
            .HasMethod(CrmSchemaNames.GinMethod)
            .HasOperators(CrmSchemaNames.TrigramOperatorClass);
        builder.HasIndex(contact => contact.EmailNormalized)
            .HasDatabaseName(CrmSchemaNames.ContactEmailTrigramIndex)
            .HasMethod(CrmSchemaNames.GinMethod)
            .HasOperators(CrmSchemaNames.TrigramOperatorClass);
        builder.HasIndex(contact => contact.CustomFields)
            .HasDatabaseName(CrmSchemaNames.ContactCustomFieldsIndex)
            .HasMethod(CrmSchemaNames.GinMethod)
            .HasOperators(CrmSchemaNames.JsonPathOperatorClass);

        builder.HasMany(contact => contact.TagLinks)
            .WithOne()
            .HasForeignKey(link => link.ContactId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
