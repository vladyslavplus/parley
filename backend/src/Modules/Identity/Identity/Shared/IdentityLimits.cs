namespace Parley.Identity.Shared;

/// <summary>
/// Input and column size limits for the identity module. Validators and column
/// mappings read the same values so a request cannot pass validation and then
/// overflow a column.
/// </summary>
public static class IdentityLimits
{
    public const int EmailMaximumLength = 256;

    public const int OrganizationNameMaximumLength = 200;

    public const int SlugMaximumLength = 64;

    public const int TimeZoneMaximumLength = 64;

    public const int CurrencyCodeLength = 3;

    public const int DisplayNameMaximumLength = 100;

    public const int ThemeMaximumLength = 16;

    public const int LocaleMaximumLength = 16;

    public const int StatusMaximumLength = 32;

    public const int TokenMaximumLength = 512;

    public const int ConfirmationTokenMaximumLength = 2048;

    public const int TeamNameMaximumLength = 100;

    public const int RoleCodeMaximumLength = 64;

    public const int PermissionMaximumLength = 64;

    public const int ScopeMaximumLength = 16;

    public const int AuditActionMaximumLength = 128;

    public const int TargetTypeMaximumLength = 64;
}
