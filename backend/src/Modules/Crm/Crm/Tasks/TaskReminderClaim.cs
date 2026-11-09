using Microsoft.EntityFrameworkCore;
using Parley.Crm.Persistence;

namespace Parley.Crm.Tasks;

/// <summary>
/// Claims the due-soon reminder of one task. The claim repeats the whole reminder predicate, so a task completed,
/// deleted, moved out of the window, or already reminded between the read and the claim is not announced.
/// </summary>
public static class TaskReminderClaim
{
    /// <summary>Sets the reminder instant when the task still qualifies; returns the number of rows claimed (0 or 1).</summary>
    public static Task<int> ClaimAsync(CrmDbContext dbContext, Guid taskId, DateTime now, DateTime windowEnd, CancellationToken cancellationToken) =>
        dbContext.Tasks
            .Where(row => row.Id == taskId
                && row.CompletedAt == null
                && row.DeletedAt == null
                && row.ReminderSentAt == null
                && row.DueAt != null
                && row.DueAt <= windowEnd)
            .ExecuteUpdateAsync(setters => setters.SetProperty(row => row.ReminderSentAt, now), cancellationToken);
}
