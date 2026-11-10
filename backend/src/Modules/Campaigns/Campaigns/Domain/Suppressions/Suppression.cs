using Parley.SharedKernel.Persistence.Entities;

namespace Parley.Campaigns.Domain.Suppressions;

/// <summary>An address that must not receive campaign email. Suppressed contacts enter neither arm.</summary>
public sealed class Suppression : BusinessEntity
{
    public Guid ContactId { get; set; }

    /// <summary>Normalized (lowercase) address the suppression applies to.</summary>
    public string EmailNormalized { get; set; } = string.Empty;

    /// <summary><see cref="SuppressionReasons"/> value.</summary>
    public string Reason { get; set; } = string.Empty;

    public DateTime SuppressedAt { get; set; }
}
