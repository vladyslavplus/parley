using Parley.Identity.Contracts.Memberships;

namespace Parley.Identity.Features.Members.Shared;

/// <summary>Sort fields and status filter values of the member list.</summary>
public static class MemberListRules
{
    public const string SortName = "name";

    public const string SortEmail = "email";

    public const string SortRole = "role";

    public const string SortJoinedAt = "joinedAt";

    public const string StatusActive = MembershipStatuses.Active;

    public const string StatusDeactivated = MembershipStatuses.Deactivated;

    public const string StatusAll = "all";

    public static IReadOnlyList<string> SortFields { get; } = [SortName, SortEmail, SortRole, SortJoinedAt];

    public static IReadOnlyList<string> Statuses { get; } = [StatusActive, StatusDeactivated, StatusAll];

    /// <summary>True when the value is absent or one of the sort fields.</summary>
    public static bool IsValidSort(string? value) =>
        string.IsNullOrEmpty(value) || value is SortName or SortEmail or SortRole or SortJoinedAt;

    /// <summary>True when the value is absent or one of the status filter values.</summary>
    public static bool IsValidStatus(string? value) =>
        string.IsNullOrEmpty(value) || value is StatusActive or StatusDeactivated or StatusAll;
}
