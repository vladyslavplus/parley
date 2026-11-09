namespace Parley.Identity.Features.Invitations.Shared;

/// <summary>Sort fields of the pending invitation list.</summary>
public static class InvitationListRules
{
    public const string SortEmail = "email";

    public const string SortCreatedAt = "createdAt";

    public const string SortExpiresAt = "expiresAt";

    public static IReadOnlyList<string> SortFields { get; } = [SortEmail, SortCreatedAt, SortExpiresAt];

    /// <summary>True when the value is absent or one of the sort fields.</summary>
    public static bool IsValidSort(string? value) =>
        string.IsNullOrEmpty(value) || value is SortEmail or SortCreatedAt or SortExpiresAt;
}
