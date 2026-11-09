namespace Parley.Crm.Shared.Text;

/// <summary>Display form of a person's name built from its parts.</summary>
public static class PersonNames
{
    /// <summary>First and last name joined by a space; blank parts are skipped, and null when both are blank.</summary>
    public static string? Join(string? firstName, string? lastName)
    {
        var parts = new[] { firstName, lastName }.Where(part => !string.IsNullOrWhiteSpace(part)).ToArray();
        return parts.Length == 0 ? null : string.Join(" ", parts);
    }
}
