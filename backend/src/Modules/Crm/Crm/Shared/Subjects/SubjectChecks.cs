using Microsoft.EntityFrameworkCore;
using Parley.Crm.Features.Deals.Shared;
using Parley.Crm.Persistence;
using Parley.Identity.Contracts.Authorization;

namespace Parley.Crm.Shared.Subjects;

/// <summary>
/// Checks the records a note, task or timeline query links to. A record counts as present only when the caller
/// may read it, so a link to a record outside the caller's scope is reported like a missing one.
/// </summary>
public static class SubjectChecks
{
    /// <summary>How many of the three subject links are set.</summary>
    public static int CountOf(Guid? contactId, Guid? dealId, Guid? companyId) =>
        (contactId is null ? 0 : 1) + (dealId is null ? 0 : 1) + (companyId is null ? 0 : 1);

    /// <summary>The field name of the first set link the caller may not read, or null when every set link is visible.</summary>
    public static async Task<string?> FirstUnreadableAsync(
        CrmDbContext dbContext,
        IDataScope dataScope,
        Guid? contactId,
        Guid? dealId,
        Guid? companyId,
        CancellationToken cancellationToken)
    {
        if (contactId is { } contact
            && !await (await DealLookup.VisibleContactsAsync(dbContext, dataScope, cancellationToken)).AnyAsync(row => row.Id == contact, cancellationToken))
        {
            return SubjectFields.ContactId;
        }

        if (dealId is { } deal
            && !await (await DealLookup.VisibleDealsAsync(dbContext, dataScope, cancellationToken)).AnyAsync(row => row.Id == deal, cancellationToken))
        {
            return SubjectFields.DealId;
        }

        if (companyId is { } company
            && !await (await DealLookup.VisibleCompaniesAsync(dbContext, dataScope, cancellationToken)).AnyAsync(row => row.Id == company, cancellationToken))
        {
            return SubjectFields.CompanyId;
        }

        return null;
    }
}
