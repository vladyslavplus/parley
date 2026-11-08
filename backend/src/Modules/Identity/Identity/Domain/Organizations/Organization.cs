namespace Parley.Identity.Domain.Organizations;

/// <summary>
/// The tenant itself. Organizations are platform rows: they are the tenant
/// boundary, not rows inside one, so they carry no <c>organization_id</c> and no
/// tenant query filter.
/// </summary>
public sealed class Organization
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Short unique identifier of the organization, assigned at creation.</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>IANA time zone of the organization; defines "today" for scheduled work.</summary>
    public string TimeZone { get; set; } = string.Empty;

    /// <summary>Default locale of members and of organization-level messages.</summary>
    public string DefaultLocale { get; set; } = string.Empty;

    /// <summary>ISO 4217 code of the single currency of the organization.</summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>Lifecycle state. Only <c>active</c> is used until organization management arrives.</summary>
    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
