namespace Parley.Api.Controllers.Identity;

/// <summary>Route templates of the identity endpoints (profile, organization, members, teams, invitations, audit).</summary>
public static class IdentityRoutes
{
    public const string Me = "me";

    public const string MeProfile = "me/profile";

    public const string MeOrganizations = "me/organizations";

    public const string OrganizationById = "organizations/{id:guid}";

    public const string OrganizationSwitch = "organizations/{id:guid}/switch";

    public const string Members = "members";

    public const string MemberRole = "members/{id:guid}/role";

    public const string MemberTeam = "members/{id:guid}/team";

    public const string MemberDeactivate = "members/{id:guid}/deactivate";

    public const string Teams = "teams";

    public const string TeamById = "teams/{id:guid}";

    public const string Invitations = "invitations";

    public const string InvitationRevoke = "invitations/{id:guid}/revoke";

    public const string InvitationAccept = "invitations/accept";

    public const string AuditEvents = "audit-events";
}
