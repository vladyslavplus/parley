namespace Parley.Identity.Contracts.Audit;

/// <summary>
/// Action codes written to the audit log. Each code is a constant; rows carry the
/// code, the actor and the target identifiers, never personal payloads.
/// </summary>
public static class AuditActions
{
    public const string MemberRoleChanged = "identity.member.role_changed";

    public const string MemberTeamChanged = "identity.member.team_changed";

    public const string MemberDeactivated = "identity.member.deactivated";

    public const string MemberReactivated = "identity.member.reactivated";

    public const string InvitationCreated = "identity.invitation.created";

    public const string InvitationAccepted = "identity.invitation.accepted";

    public const string InvitationRevoked = "identity.invitation.revoked";

    public const string TeamCreated = "identity.team.created";

    public const string TeamRenamed = "identity.team.renamed";

    public const string TeamDeleted = "identity.team.deleted";

    public const string OrganizationSettingsChanged = "identity.organization.settings_changed";

    public const string SignInFailed = "identity.session.sign_in_failed";

    public const string AccountLocked = "identity.session.account_locked";
}
