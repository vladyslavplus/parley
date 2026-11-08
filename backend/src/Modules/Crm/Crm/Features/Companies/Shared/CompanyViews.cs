using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Parley.Crm.Domain.Companies;
using Parley.Crm.Shared.CustomFields;
using Parley.SharedKernel.Persistence.Conventions;

namespace Parley.Crm.Features.Companies.Shared;

/// <summary>Projection and mapping of companies.</summary>
public static class CompanyViews
{
    public static Expression<Func<Company, CompanyRow>> RowProjection => company => new CompanyRow(
        company.Id,
        company.Name,
        company.Domain,
        company.Country,
        company.Industry,
        company.OwnerUserId,
        company.CustomFields,
        company.CreatedAt,
        company.UpdatedAt,
        EF.Property<uint>(company, PersistenceConstants.XminPropertyName),
        company.NameNormalized);

    public static CompanyView FromEntity(Company company, uint version) => new(
        company.Id,
        company.Name,
        company.Domain,
        company.Country,
        company.Industry,
        company.OwnerUserId,
        CustomFieldValues.ToMap(company.CustomFields),
        company.CreatedAt,
        company.UpdatedAt,
        version);

    public static CompanyView ToView(CompanyRow row) => new(
        row.Id,
        row.Name,
        row.Domain,
        row.Country,
        row.Industry,
        row.OwnerUserId,
        CustomFieldValues.ToMap(row.CustomFields),
        row.CreatedAt,
        row.UpdatedAt,
        row.Version);
}
