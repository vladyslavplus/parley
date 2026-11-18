namespace Parley.Crm.Shared.Pipelines;

/// <summary>
/// The five-step blue ramp of the interface and the stage colours of the default pipeline. Six-digit hex is the
/// stored form, so the values match the light-theme stage tokens of the frontend.
/// </summary>
public static class PipelineStageColors
{
    public const string Lightest = "#e6ecf5";

    public const string Light = "#c9d7eb";

    public const string Medium = "#93b2dc";

    public const string Deep = "#3866a0";

    public const string Strong = "#2453be";

    /// <summary>The ramp in order, lightest to strongest.</summary>
    public static IReadOnlyList<string> Defaults { get; } = [Lightest, Light, Medium, Deep, Strong];

    /// <summary>The lowercase form written to the stage row.</summary>
    public static string Normalize(string color) => color.ToLowerInvariant();
}
