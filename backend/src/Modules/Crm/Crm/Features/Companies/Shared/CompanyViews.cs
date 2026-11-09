using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Parley.Crm.Domain.Companies;
using Parley.Crm.Shared.CustomFields;
using Parley.Crm.Shared.Owners;
using Parley.Identity.Contracts.Users;
using Parley.SharedKernel.Persistence.Conventions;

namespace Parley.Crm.Features.Companies.Shared;

/// <summary>Projection and mapping of companies. The owner name comes from the user directory, once per page.</summary>
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

    /// <summary>Maps one row, resolving its owner with a single directory call.</summary>
    public static async Task<CompanyView> ToViewAsync(CompanyRow row, OwnerDirectory owners, CancellationToken cancellationToken) =>
        ToView(row, await owners.ReadAsync([row.OwnerUserId], cancellationToken));

    public static CompanyView ToView(CompanyRow row, IReadOnlyDictionary<Guid, UserDirectoryEntry> owners)
    {
        var owner = OwnerDirectory.Find(owners, row.OwnerUserId);
        return new CompanyView(
            row.Id,
            row.Name,
            row.Domain,
            row.Country,
            row.Industry,
            row.OwnerUserId,
            CustomFieldValues.ToMap(row.CustomFields),
            row.CreatedAt,
            row.UpdatedAt,
            row.Version,
            owner?.DisplayName,
            owner?.Email);
    }
}
