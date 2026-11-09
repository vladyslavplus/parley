using Microsoft.EntityFrameworkCore;
using Parley.Crm.Authorization.Scope;
using Parley.Crm.Domain.Tasks;
using Parley.Crm.Features.Deals.Shared;
using Parley.Crm.Persistence;
using Parley.Crm.Shared.Tasks;
using Parley.Identity.Contracts.Authorization;

namespace Parley.Crm.Features.Tasks.Shared;

/// <summary>Scoped task lookups shared by the task handlers.</summary>
internal static class TaskLookup
{
    /// <summary>A tracked task the caller may change, or null when missing, deleted or out of scope.</summary>
    public static Task<CrmTask?> FindTrackedAsync(CrmDbContext dbContext, Guid id, DataScope scope, CancellationToken cancellationToken) =>
        TaskScopeFilter.Apply(dbContext.Tasks.Where(task => task.Id == id), scope).FirstOrDefaultAsync(cancellationToken);

    /// <summary>A task row the caller may read, or null when missing, deleted or out of scope.</summary>
    public static async Task<TaskRow?> FindRowAsync(
        CrmDbContext dbContext,
        IDataScope dataScope,
        Guid id,
        DataScope scope,
        CancellationToken cancellationToken)
    {
        var visibleContacts = await DealLookup.VisibleContactsAsync(dbContext, dataScope, cancellationToken);
        var visibleCompanies = await DealLookup.VisibleCompaniesAsync(dbContext, dataScope, cancellationToken);
        var visibleDeals = await DealLookup.VisibleDealsAsync(dbContext, dataScope, cancellationToken);
        return await TaskScopeFilter.Apply(dbContext.Tasks.AsNoTracking().Where(task => task.Id == id), scope)
            .Select(TaskViews.RowProjectionFor(visibleContacts, visibleCompanies, visibleDeals))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
