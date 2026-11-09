using Parley.SharedKernel.Persistence.Entities;

namespace Parley.Crm.Domain.Deals;

/// <summary>
/// One stage change of a deal, append-only. <see cref="FromStageId"/> is null for the stage a deal was created on.
/// </summary>
public sealed class DealStageHistoryEntry : BusinessEntity
{
    public Guid DealId { get; set; }

    public Guid? FromStageId { get; set; }

    public Guid ToStageId { get; set; }

    public Guid ChangedByUserId { get; set; }

    public DateTime ChangedAt { get; set; }
}
