namespace Parley.Identity.Domain.Users;

/// <summary>
/// Stored values of the theme preference of a user.
/// </summary>
public static class UserThemes
{
    public const string System = "system";

    public const string Light = "light";

    public const string Dark = "dark";

    public static IReadOnlyList<string> All { get; } = [System, Light, Dark];
}
