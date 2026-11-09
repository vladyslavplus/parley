using System.Runtime.CompilerServices;

namespace Parley.IntegrationTests.Infrastructure.Hosting;

/// <summary>
/// Turns the OpenTelemetry SDK off for the integration test process. The OTLP exporters flush on every host shutdown to
/// a collector that the tests do not run, which costs several seconds per test host. No integration test reads
/// telemetry, so the SDK is disabled before any host is built. Unit tests of the telemetry helpers are unaffected.
/// </summary>
internal static class TestTelemetryBootstrap
{
    private const string SdkDisabledVariable = "OTEL_SDK_DISABLED";

    private const string TrueValue = "true";

    [ModuleInitializer]
    internal static void DisableOpenTelemetrySdk() => Environment.SetEnvironmentVariable(SdkDisabledVariable, TrueValue);
}
