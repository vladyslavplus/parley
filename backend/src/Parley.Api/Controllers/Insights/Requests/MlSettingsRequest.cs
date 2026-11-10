namespace Parley.Api.Controllers.Insights.Requests;

/// <summary>Body of an ML settings change.</summary>
/// <param name="MlEnabled">The new state of the switch; required.</param>
/// <param name="Version">Row version the client read; 0 or omitted when the organization had no row.</param>
public sealed record MlSettingsRequest(bool? MlEnabled, uint? Version);
