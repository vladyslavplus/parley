namespace Parley.Insights.Features.MlSettings.Shared;

/// <summary>The ML switch of the organization, the stored model choices, the model each kind scores with now, and the row version.</summary>
/// <param name="MlEnabled">True when ML scoring is switched on.</param>
/// <param name="ActiveUpliftVersion">Uplift model the organization chose; null means the newest synced uplift model.</param>
/// <param name="ActiveClvVersion">CLV model the organization chose; null means the newest synced CLV model.</param>
/// <param name="Version">Row version; 0 while the organization has no settings row yet.</param>
/// <param name="EffectiveUpliftVersion">Uplift version the runs and the priority list use now; null when the registry holds none.</param>
/// <param name="EffectiveClvVersion">CLV version the runs use now; null when the registry holds none.</param>
public sealed record MlSettingsView(
    bool MlEnabled,
    string? ActiveUpliftVersion,
    string? ActiveClvVersion,
    uint Version,
    string? EffectiveUpliftVersion,
    string? EffectiveClvVersion);
