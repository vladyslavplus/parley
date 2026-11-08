using Parley.Crm.Shared;
using Parley.SharedKernel.Persistence.Entities;

namespace Parley.Crm.Domain.Companies;

/// <summary>
/// An organization a contact can belong to. Soft deleted through <see cref="DeletedAt"/>.
/// </summary>
public sealed class Company : BusinessEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Domain { get; set; }

    /// <summary>ISO 3166-1 alpha-2 code, uppercase.</summary>
    public string? Country { get; set; }

    public string? Industry { get; set; }

    public Guid? OwnerUserId { get; set; }

    /// <summary>Custom field values as a JSON object keyed by definition key.</summary>
    public string CustomFields { get; set; } = CrmLimits.EmptyJsonObject;

    public DateTime? DeletedAt { get; set; }

    /// <summary>Lowercase name, a stored generated column used for sorting and trigram search.</summary>
    public string NameNormalized { get; private set; } = string.Empty;
}
