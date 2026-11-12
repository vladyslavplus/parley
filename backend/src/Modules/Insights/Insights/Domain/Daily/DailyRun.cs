using Parley.SharedKernel.Persistence.Entities;

namespace Parley.Insights.Domain.Daily;

/// <summary>
/// The completion state of one organization day. The RFM step stamps <see cref="RfmCompletedAt"/> after its rows are
/// written, and the priority step stamps <see cref="PriorityCompletedAt"/> after its rows are written. Readers take only
/// days whose stamp is set, so a half-written day is never visible.
/// </summary>
public sealed class DailyRun : BusinessEntity
{
    /// <summary>The organization's calendar day the run computes.</summary>
    public DateOnly AsOf { get; set; }

    /// <summary>Set once every RFM snapshot row of the day is written and the stale rows are removed.</summary>
    public DateTime? RfmCompletedAt { get; set; }

    /// <summary>Set once the priority rows of the day are written.</summary>
    public DateTime? PriorityCompletedAt { get; set; }

    /// <summary>The mode the priority list was built in: ml when the uplift scores were used, rfm otherwise.</summary>
    public string? PriorityMode { get; set; }

    /// <summary>Why the list is in its mode, from <see cref="Parley.Insights.Domain.Priority.PriorityMlStatus"/>.</summary>
    public string? PriorityMlStatus { get; set; }
}
