namespace Parley.Identity.Contracts.Audit;

/// <summary>
/// Entity type names stored with audit rows, so an event points at the record it changed.
/// </summary>
public static class AuditTargetTypes
{
    public const string Member = "member";

    public const string Team = "team";

    public const string Invitation = "invitation";

    public const string Organization = "organization";

    public const string User = "user";
}
