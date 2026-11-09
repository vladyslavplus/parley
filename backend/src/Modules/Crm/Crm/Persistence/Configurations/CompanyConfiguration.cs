using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NpgsqlTypes;
using Parley.Crm.Domain.Companies;
using Parley.Crm.Shared;
using Parley.SharedKernel.Persistence.Conventions;

namespace Parley.Crm.Persistence.Configurations;

/// <summary>Mapping of <see cref="Company"/>: soft delete, keyset and search indexes.</summary>
public sealed class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    private static readonly ValueConverter<DateTime?, DateTime?> NullableUtcConverter = new(
        value => value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : value,
        value => value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : value);

    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable(CrmSchemaNames.CompaniesTable);
        builder.Property(company => company.Name).HasMaxLength(CrmLimits.CompanyNameMaximumLength).IsRequired();
        builder.Property(company => company.Domain).HasMaxLength(CrmLimits.DomainMaximumLength);
        builder.Property(company => company.Country).HasColumnType("char(2)");
        builder.Property(company => company.Industry).HasMaxLength(CrmLimits.IndustryMaximumLength);
        builder.Property(company => company.CustomFields).HasColumnType("jsonb").IsRequired();
        builder.Property(company => company.DeletedAt).HasColumnType("timestamptz").HasConversion(NullableUtcConverter);
        builder.Property(company => company.NameNormalized)
            .HasMaxLength(CrmLimits.CompanyNameMaximumLength)
            .HasComputedColumnSql(CrmSql.CompanyNameNormalized, stored: true);
        builder.Property<NpgsqlTsVector>(CrmSchemaNames.SearchVectorProperty)
            .HasColumnName(CrmSchemaNames.SearchVectorColumn)
            .HasComputedColumnSql(CrmSql.CompanySearchVector, stored: true);


        builder.HasTenantIndex(CrmSchemaNames.CompanyIdIndex, nameof(Company.Id));
        builder.HasTenantIndex(CrmSchemaNames.CompanyOwnerIndex, nameof(Company.OwnerUserId), nameof(Company.Id));
        builder.HasTenantIndex(CrmSchemaNames.CompanyCreatedIndex, nameof(Company.CreatedAt), nameof(Company.Id));
        builder.HasTenantIndex(CrmSchemaNames.CompanyNameIndex, nameof(Company.NameNormalized), nameof(Company.Id));
        builder.HasIndex(CrmSchemaNames.SearchVectorProperty)
            .HasDatabaseName(CrmSchemaNames.CompanySearchIndex)
            .HasMethod(CrmSchemaNames.GinMethod);
        builder.HasIndex(company => company.NameNormalized)
            .HasDatabaseName(CrmSchemaNames.CompanyNameTrigramIndex)
            .HasMethod(CrmSchemaNames.GinMethod)
            .HasOperators(CrmSchemaNames.TrigramOperatorClass);
        builder.HasIndex(company => company.CustomFields)
            .HasDatabaseName(CrmSchemaNames.CompanyCustomFieldsIndex)
            .HasMethod(CrmSchemaNames.GinMethod)
            .HasOperators(CrmSchemaNames.JsonPathOperatorClass);
    }
}
