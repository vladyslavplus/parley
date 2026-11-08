namespace Parley.Identity.Contracts.Authorization;

/// <summary>
/// Codes of the five system roles. A member has exactly one role per organization.
/// </summary>
public static class SystemRoles
{
    public const string Admin = "admin";
    public const string SalesManager = "sales_manager";
    public const string SalesRep = "sales_rep";
    public const string Marketer = "marketer";
    public const string Viewer = "viewer";
}
