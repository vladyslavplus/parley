using Parley.SharedKernel.Persistence.Entities;

namespace Parley.Crm.Domain.Tasks;

/// <summary>
/// A follow-up assigned to a member, optionally linked to a contact, a deal or a company. Soft deleted through
/// <see cref="DeletedAt"/>. <see cref="ReminderSentAt"/> is set once the due-soon reminder has been published.
/// </summary>
public sealed class CrmTask : BusinessEntity
{
    public string Title { get; set; } = string.Empty;

    /// <summary>Lowercase copy of <see cref="Title"/> that backs title sorting and search.</summary>
    public string TitleNormalized { get; private set; } = string.Empty;

    public string? Notes { get; set; }

    public DateTime? DueAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public Guid AssigneeUserId { get; set; }

    public Guid? ContactId { get; set; }

    public Guid? DealId { get; set; }

    public Guid? CompanyId { get; set; }

    public DateTime? ReminderSentAt { get; set; }

    public DateTime? DeletedAt { get; set; }
}
