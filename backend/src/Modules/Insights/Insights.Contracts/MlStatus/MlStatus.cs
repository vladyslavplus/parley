namespace Parley.Insights.Contracts.MlStatus;

/// <summary>The ML switch of an organization and the model versions it is set to use.</summary>
/// <param name="MlEnabled">True when the organization uses ML scores; false means the RFM mode everywhere.</param>
/// <param name="ActiveUpliftVersion">Active uplift model version, or null when none is set.</param>
/// <param name="ActiveClvVersion">Active CLV model version, or null when none is set.</param>
public sealed record MlStatus(bool MlEnabled, string? ActiveUpliftVersion, string? ActiveClvVersion);
