using Microsoft.AspNetCore.Identity;

namespace Parley.Identity.Domain.Users;

/// <summary>
/// Platform account. Users are not tenant rows: one person can belong to several
/// organizations, so the table carries no <c>organization_id</c> and no tenant
/// query filter. The organization a request acts for is carried in the access
/// token, and membership is checked per request when memberships arrive.
/// </summary>
public sealed class User : IdentityUser<Guid>
{
    /// <summary>Name shown in the interface. Optional; empty means the address is shown instead.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Locale of the account owner, used for account email language and the interface.</summary>
    public string Locale { get; set; } = string.Empty;

    /// <summary>IANA time zone the user sees dates in.</summary>
    public string TimeZone { get; set; } = UserDefaults.TimeZone;

    /// <summary>One of the values in <see cref="UserThemes"/>.</summary>
    public string Theme { get; set; } = UserThemes.System;

    /// <summary>Organization used when a token is issued without an explicit switch.</summary>
    public Guid? LastActiveOrganizationId { get; set; }

    /// <summary>
    /// Platform operator flag. Carried as a claim in access tokens and the only
    /// gate of the Hangfire dashboard. Set outside the API; never by organization admins.
    /// </summary>
    public bool IsPlatformOperator { get; set; }
}
