using Parley.Crm.Shared;
using Parley.SharedKernel.Persistence.Entities;

namespace Parley.Crm.Domain.Activities;

/// <summary>
/// Append-only timeline entry of a contact. Holds identifiers and constant codes only.
/// </summary>
public sealed class Activity : BusinessEntity
{
    public string Type { get; set; } = string.Empty;

    public Guid? ContactId { get; set; }

    public Guid? ActorUserId { get; set; }

    /// <summary>Safe payload as a JSON object; never personal data.</summary>
    public string Payload { get; set; } = CrmLimits.EmptyJsonObject;

    public DateTime OccurredAt { get; set; }

    /// <summary>Identifier of the integration event that produced the row; unique so replays are no-ops.</summary>
    public Guid? SourceEventId { get; set; }
}
