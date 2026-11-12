namespace Parley.Insights.Features.MlSettings.Update;

/// <summary>
/// A requested uplift model choice. A null <see cref="Version"/> resets the organization to the newest synced uplift model.
/// </summary>
/// <param name="Version">Version of the uplift model to use; null or empty means the newest synced one.</param>
public sealed record ActiveUpliftChange(string? Version);
