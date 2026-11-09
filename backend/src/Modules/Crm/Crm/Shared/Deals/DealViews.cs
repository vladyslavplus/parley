using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Parley.Crm.Domain.Companies;
using Parley.Crm.Domain.Contacts;
using Parley.Crm.Domain.Deals;
using Parley.Crm.Domain.Pipelines;
using Parley.Crm.Features.Deals.Shared;
using Parley.Crm.Shared.CustomFields;
using Parley.Crm.Shared.Owners;
using Parley.Crm.Shared.Pipelines;
using Parley.Crm.Shared.Text;
using Parley.Identity.Contracts.Users;
using Parley.SharedKernel.Persistence.Conventions;

namespace Parley.Crm.Shared.Deals;

/// <summary>
/// Projection and mapping of deals. One statement reads a deal with the names of its stage, its contact and its
/// company, limited to the records the caller may see. The owner name comes from the user directory, one call per page.
/// </summary>
public static class DealViews
{
    /// <summary>
    /// Row projection. The contact and company names are read only from the records the caller may see; a link to
    /// a record outside the caller's scope shows no name.
    /// </summary>
    public static Expression<Func<Deal, DealRow>> RowProjectionFor(
        IQueryable<Contact> visibleContacts,
        IQueryable<Company> visibleCompanies,
        IQueryable<PipelineStage> stages) => deal => new DealRow(
        deal.Id,
        deal.PipelineId,
        deal.StageId,
        stages.Where(stage => stage.Id == deal.StageId).Select(stage => stage.Name).FirstOrDefault(),
        stages.Where(stage => stage.Id == deal.StageId).Select(stage => stage.SystemKey).FirstOrDefault(),
        deal.Status,
        deal.Title,
        deal.TitleNormalized,
        deal.Amount,
        deal.Currency,
        deal.ContactId,
        visibleContacts.Where(contact => contact.Id == deal.ContactId).Select(contact => contact.FirstName).FirstOrDefault(),
        visibleContacts.Where(contact => contact.Id == deal.ContactId).Select(contact => contact.LastName).FirstOrDefault(),
        deal.CompanyId,
        visibleCompanies.Where(company => company.Id == deal.CompanyId).Select(company => company.Name).FirstOrDefault(),
        deal.OwnerUserId,
        deal.LostReason,
        deal.ExpectedCloseOn,
        deal.ClosedAt,
        deal.BoardPosition,
        deal.CustomFields,
        deal.CreatedAt,
        deal.UpdatedAt,
        EF.Property<uint>(deal, PersistenceConstants.XminPropertyName));

    /// <summary>Maps one row, resolving its owner with a single directory call.</summary>
    public static async Task<DealView> ToViewAsync(DealRow row, OwnerDirectory owners, CancellationToken cancellationToken) =>
        ToView(row, await owners.ReadAsync([(Guid?)row.OwnerUserId], cancellationToken));

    public static DealView ToView(DealRow row, IReadOnlyDictionary<Guid, UserDirectoryEntry> owners)
    {
        var owner = OwnerDirectory.Find(owners, row.OwnerUserId);
        return new DealView(
            row.Id,
            row.PipelineId,
            row.StageId,
            PipelineNames.Display(row.StageName ?? string.Empty, row.StageSystemKey),
            row.Status,
            row.Title,
            row.Amount,
            row.Currency,
            row.ContactId,
            PersonNames.Join(row.ContactFirstName, row.ContactLastName),
            row.CompanyId,
            row.CompanyName,
            row.OwnerUserId,
            owner?.DisplayName,
            owner?.Email,
            row.ExpectedCloseOn,
            row.ClosedAt,
            row.LostReason,
            row.BoardPosition,
            CustomFieldValues.ToMap(row.CustomFields),
            row.CreatedAt,
            row.UpdatedAt,
            row.Version);
    }
}
