using Parley.SharedKernel.Persistence.Entities;

namespace Parley.Insights.Domain.Priority;

/// <summary>One contact of the priority list of an organization day. Rebuilt by the build job for that day.</summary>
public sealed class PriorityItem : BusinessEntity
{
    public DateOnly AsOf { get; set; }

    public Guid? OwnerUserId { get; set; }

    public Guid ContactId { get; set; }

    public int Rank { get; set; }

    /// <summary>Scoring mode: <c>rfm</c> until the ML scores exist.</summary>
    public string Mode { get; set; } = string.Empty;

    public decimal PriorityValue { get; set; }

    public string[] ReasonCodes { get; set; } = [];

    public string SuggestedAction { get; set; } = string.Empty;

    public string Segment { get; set; } = string.Empty;

    /// <summary>Uplift of the contact in ML mode, as the scoring run estimated it; null in RFM mode and for a contact without a score.</summary>
    public decimal? UpliftValue { get; set; }

    /// <summary>Uplift segment the model gave the contact in ML mode; null in RFM mode and for a contact without a score.</summary>
    public string? UpliftSegment { get; set; }
}
