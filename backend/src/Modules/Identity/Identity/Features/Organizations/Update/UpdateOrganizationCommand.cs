using Mediator;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Identity.Features.Organizations.Update;

/// <summary>
/// Updates the name, time zone, default locale and currency of the current organization.
/// The slug is assigned once and does not change.
/// </summary>
/// <param name="OrganizationId">Organization identifier; must be the current organization.</param>
/// <param name="Name">Display name.</param>
/// <param name="TimeZone">IANA time zone.</param>
/// <param name="DefaultLocale">uk or en.</param>
/// <param name="Currency">ISO 4217 code.</param>
[RequiresPermission(Permissions.OrgManage)]
public sealed record UpdateOrganizationCommand(Guid OrganizationId, string Name, string TimeZone, string DefaultLocale, string Currency) : IRequest<Result>;
