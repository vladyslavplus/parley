using Microsoft.Extensions.Options;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Parley.Api.Options;
using Parley.Notifications.Telegram.Bot;
using Parley.SharedKernel.Logging;
using Parley.SharedKernel.Observability;

namespace Parley.Api.Startup;

public static class ObservabilityServiceCollectionExtensions
{
    /// <summary>
    /// Registers OpenTelemetry tracing, metrics and logging exporters.
    /// </summary>
    public static IServiceCollection AddObservability(this IServiceCollection services)
    {
        services.AddOptions<OtlpExporterOptions>()
            .Configure<IOptions<TelemetryOptions>>((exporterOptions, telemetryOptions) =>
                exporterOptions.Endpoint = new Uri(telemetryOptions.Value.OtlpEndpoint, UriKind.Absolute));

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(ApiConstants.ServiceName))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation(http =>
                    // Telegram puts the bot token in the request path: the recorded URL is rewritten before the span is exported.
                    http.EnrichWithHttpRequestMessage = (activity, request) =>
                        TelegramTraceRedaction.Redact(activity, request.RequestUri))
                .AddSource(OrganizationTelemetry.ActivitySourceName)
                .AddOtlpExporter())
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddOtlpExporter())
            .WithLogging(logging => logging
                .AddProcessor(new PersonalDataLogRecordProcessor())
                .AddOtlpExporter());
        return services;
    }
}
