using Microsoft.EntityFrameworkCore;
using Parley.Crm.Contracts.Dashboard;
using Parley.Crm.Domain.Deals;
using Parley.Crm.Persistence;

namespace Parley.Crm.Readers;

/// <summary>
/// Dashboard totals: open deals grouped by stage and the count of due open tasks. Both queries run on the live rows of the
/// tenant, and an owner list of nothing returns nothing instead of falling back to the whole organization.
/// </summary>
public sealed class CrmDashboardReader(CrmDbContext dbContext) : ICrmDashboardReader
{
    public async Task<IReadOnlyList<CrmPipelineStageTotal>> ReadOpenPipelineAsync(
        IReadOnlyCollection<Guid>? ownerUserIds,
        CancellationToken cancellationToken)
    {
        if (ownerUserIds is { Count: 0 })
        {
            return [];
        }

        var deals = dbContext.Deals
            .AsNoTracking()
            .Where(deal => deal.DeletedAt == null && deal.Status == DealStatuses.Open);
        if (ownerUserIds is not null)
        {
            var owners = ownerUserIds.ToArray();
            deals = deals.Where(deal => owners.Contains(deal.OwnerUserId));
        }

        var totals = await deals
            .Join(dbContext.PipelineStages.AsNoTracking(), deal => deal.StageId, stage => stage.Id, (deal, stage) => new { stage.Id, stage.Name, stage.Position, deal.Amount })
            .GroupBy(row => new { row.Id, row.Name, row.Position })
            .Select(group => new { group.Key.Id, group.Key.Name, group.Key.Position, Count = group.Count(), Amount = group.Sum(row => row.Amount) })
            .OrderBy(row => row.Position)
            .ThenBy(row => row.Name)
            .ToListAsync(cancellationToken);

        return totals.Select(row => new CrmPipelineStageTotal(row.Id, row.Name, row.Position, row.Count, row.Amount)).ToArray();
    }

    public async Task<int> CountTasksDueAsync(
        DateTimeOffset dueAt,
        IReadOnlyCollection<Guid>? assigneeUserIds,
        CancellationToken cancellationToken)
    {
        if (assigneeUserIds is { Count: 0 })
        {
            return 0;
        }

        var dueInstant = dueAt.UtcDateTime;
        var tasks = dbContext.Tasks
            .AsNoTracking()
            .Where(task => task.DeletedAt == null && task.CompletedAt == null && task.DueAt != null && task.DueAt <= dueInstant);
        if (assigneeUserIds is not null)
        {
            var assignees = assigneeUserIds.ToArray();
            tasks = tasks.Where(task => assignees.Contains(task.AssigneeUserId));
        }

        return await tasks.CountAsync(cancellationToken);
    }
}
