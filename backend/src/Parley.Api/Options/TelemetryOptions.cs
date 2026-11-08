namespace Parley.Api.Options;

/// <summary>
/// OpenTelemetry endpoint for the Aspire Dashboard. Overridable by an environment variable
/// such as <c>Telemetry__OtlpEndpoint</c>.
/// </summary>
public sealed class TelemetryOptions
{
    public const string SectionName = "Telemetry";

    public string OtlpEndpoint { get; set; } = string.Empty;
}
