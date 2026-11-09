using Parley.Crm.Domain.Tags;
using Parley.Crm.Shared;
using Parley.SharedKernel.Persistence.Entities;

namespace Parley.Crm.Domain.Contacts;

/// <summary>
/// A person the organization works with. Soft deleted through <see cref="DeletedAt"/>;
/// the email is unique among live contacts of an organization.
/// </summary>
public sealed class Contact : BusinessEntity
{
    public string FirstName { get; set; } = string.Empty;

    public string? LastName { get; set; }

    public string? Email { get; set; }

    /// <summary>Trimmed, lowercase copy of <see cref="Email"/> that backs the unique index.</summary>
    public string? EmailNormalized { get; set; }

    public string? Phone { get; set; }

    public string? JobTitle { get; set; }

    /// <summary>ISO 3166-1 alpha-2 code, uppercase.</summary>
    public string? Country { get; set; }

    public string? City { get; set; }

    public Guid? CompanyId { get; set; }

    public Guid? OwnerUserId { get; set; }

    public string Source { get; set; } = ContactSources.Manual;

    /// <summary>Identifier in the source system; unique per organization and source when set.</summary>
    public string? ExternalRef { get; set; }

    /// <summary>Instant of the latest non-cancellation order, UTC; maintained from the orders table.</summary>
    public DateTime? LastOrderAt { get; set; }

    /// <summary>Custom field values as a JSON object keyed by definition key.</summary>
    public string CustomFields { get; set; } = CrmLimits.EmptyJsonObject;

    public DateTime? DeletedAt { get; set; }

    /// <summary>Lowercase "first last" text, a stored generated column used for sorting and trigram search.</summary>
    public string NameNormalized { get; private set; } = string.Empty;

    public List<ContactTag> TagLinks { get; set; } = [];
}
