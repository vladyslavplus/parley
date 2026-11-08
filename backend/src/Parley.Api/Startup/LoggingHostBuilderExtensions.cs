using System.Globalization;
using Parley.SharedKernel.Logging;
using Serilog;
using Serilog.Formatting.Display;

namespace Parley.Api.Startup;

public static class LoggingHostBuilderExtensions
{
    /// <summary>
    /// Configures the host logger for the API (Serilog).
    /// </summary>
    public static void ConfigureSerilog(this WebApplicationBuilder builder)
    {
        // The console sink is configured here, not in appsettings, so its formatter can
        // mask personal data in the rendered exception chain (stack frames included).
        builder.Host.UseSerilog((hostContext, loggingConfiguration) => loggingConfiguration
            .ReadFrom.Configuration(hostContext.Configuration)
            .Enrich.With<PersonalDataScrubbingEnricher>()
            .Enrich.WithProperty("Service", ApiConstants.ServiceName)
            .WriteTo.Console(new PersonalDataScrubbingTextFormatter(
                new MessageTemplateTextFormatter(ApiConstants.ConsoleOutputTemplate, CultureInfo.InvariantCulture))),
            preserveStaticLogger: false, writeToProviders: true);
    }
}
