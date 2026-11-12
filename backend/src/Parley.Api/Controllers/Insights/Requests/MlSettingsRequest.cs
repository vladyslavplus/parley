using System.Text.Json;

namespace Parley.Api.Controllers.Insights.Requests;

/// <summary>Body of an ML settings change.</summary>
/// <param name="MlEnabled">The new state of the switch; required.</param>
/// <param name="Version">Row version the client read; 0 or omitted when the organization had no row.</param>
/// <param name="ActiveUpliftVersion">
/// The uplift model to use. Omitted keeps the stored choice, null resets to the newest synced uplift model, and a string
/// selects that version (it must be an uplift model in the registry).
/// </param>
public sealed record MlSettingsRequest(bool? MlEnabled, uint? Version, JsonElement ActiveUpliftVersion);
