using Mediator;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Identity.Features.Profile.UpdateProfile;

/// <summary>
/// Updates the profile of the signed-in user: display name, locale, time zone and theme.
/// </summary>
/// <param name="DisplayName">Name shown in the interface; null or blank clears it.</param>
/// <param name="Locale">uk or en.</param>
/// <param name="TimeZone">IANA time zone.</param>
/// <param name="Theme">system, light or dark.</param>
[PermissionOptOut(PermissionOptOutKind.AuthenticatedOnly)]
public sealed record UpdateProfileCommand(string? DisplayName, string Locale, string TimeZone, string Theme) : IRequest<Result>;
