using Parley.SharedKernel.Persistence.Entities;

namespace Parley.Crm.Domain.Notes;

/// <summary>
/// Free text written about a contact, a deal or a company. The text stays here; the timeline keeps only the note id.
/// Soft deleted through <see cref="DeletedAt"/>.
/// </summary>
public sealed class Note : BusinessEntity
{
    public string Body { get; set; } = string.Empty;

    public Guid? ContactId { get; set; }

    public Guid? DealId { get; set; }

    public Guid? CompanyId { get; set; }

    public Guid AuthorUserId { get; set; }

    public DateTime? DeletedAt { get; set; }
}
