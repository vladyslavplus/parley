namespace Parley.Campaigns.Features.Campaigns.Shared;

/// <summary>Sort fields accepted by the campaign list. Ties are broken by identifier.</summary>
public static class CampaignListSortFields
{
    /// <summary>Campaign name. Default.</summary>
    public const string Name = "name";

    /// <summary>Creation instant.</summary>
    public const string Created = "created";

    public static IReadOnlyList<string> Values { get; } = [Name, Created];

    public static bool IsValid(string? value) =>
        string.IsNullOrEmpty(value) || value is Name or Created;
}
