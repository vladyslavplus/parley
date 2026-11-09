using Parley.Crm.Shared;
using Parley.SharedKernel.Persistence.Entities;

namespace Parley.Crm.Domain.Deals;

/// <summary>
/// A sales opportunity placed on a stage of a pipeline. Soft deleted through <see cref="DeletedAt"/>.
/// <see cref="BoardPosition"/> orders the deals of one stage; it leaves gaps so a move rarely renumbers.
/// </summary>
public sealed class Deal : BusinessEntity
{
    public Guid PipelineId { get; set; }

    public Guid StageId { get; set; }

    public string Title { get; set; } = string.Empty;

    /// <summary>Lowercase copy of <see cref="Title"/> that backs name sorting.</summary>
    public string TitleNormalized { get; private set; } = string.Empty;

    public decimal Amount { get; set; }

    /// <summary>ISO 4217 code, uppercase.</summary>
    public string Currency { get; set; } = string.Empty;

    public Guid? ContactId { get; set; }

    public Guid? CompanyId { get; set; }

    public Guid OwnerUserId { get; set; }

    /// <summary>One of <see cref="DealStatuses"/>; derived from the stage kind.</summary>
    public string Status { get; set; } = DealStatuses.Open;

    public string? LostReason { get; set; }

    public DateOnly? ExpectedCloseOn { get; set; }

    public DateTime? ClosedAt { get; set; }

    public long BoardPosition { get; set; }

    /// <summary>Custom field values as a JSON object keyed by definition key.</summary>
    public string CustomFields { get; set; } = CrmLimits.EmptyJsonObject;

    public string Source { get; set; } = DealSources.Manual;

    public DateTime? DeletedAt { get; set; }
}
