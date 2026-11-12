namespace Parley.Insights.Domain.Ml;

/// <summary>
/// The score the read path uses, one per organization, contact and score kind. Not a <c>BusinessEntity</c>: the key is
/// composite, and EF keeps the key on the root type only. The tenant filter is set on the context, as the CRM contact-tag
/// link does, and the row is swapped in whole when a scoring run completes, so it has no version token.
/// </summary>
public sealed class LatestScore
{
    public Guid OrganizationId { get; set; }

    public Guid ContactId { get; set; }

    public string ScoreKind { get; set; } = string.Empty;

    public Guid ScoreId { get; set; }

    public decimal Value { get; set; }

    public string Payload { get; set; } = "{}";

    public string ModelName { get; set; } = string.Empty;

    public string ModelVersion { get; set; } = string.Empty;

    public DateTime ScoredAt { get; set; }
}
