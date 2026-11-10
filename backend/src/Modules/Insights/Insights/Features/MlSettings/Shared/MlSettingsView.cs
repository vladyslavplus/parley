namespace Parley.Insights.Features.MlSettings.Shared;

/// <summary>The ML switch of the organization and the row version the client must send back on a change.</summary>
/// <param name="MlEnabled">True when ML scoring is switched on.</param>
/// <param name="ActiveUpliftVersion">Uplift model in use; null when none is active.</param>
/// <param name="ActiveClvVersion">CLV model in use; null when none is active.</param>
/// <param name="Version">Row version; 0 while the organization has no settings row yet.</param>
public sealed record MlSettingsView(bool MlEnabled, string? ActiveUpliftVersion, string? ActiveClvVersion, uint Version);
