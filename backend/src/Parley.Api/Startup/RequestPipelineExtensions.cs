using Parley.SharedKernel.Localization;
using Serilog;

namespace Parley.Api.Startup;

public static class RequestPipelineExtensions
{
    /// <summary>
    /// Adds the request middleware in the order the API depends on.
    /// </summary>
    public static void UseRequestPipeline(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseRequestLocalization(options =>
        {
            options.SetDefaultCulture(CultureCodes.Default);
            options.AddSupportedCultures(CultureCodes.Ukrainian, CultureCodes.English);
            options.AddSupportedUICultures(CultureCodes.Ukrainian, CultureCodes.English);
            options.ApplyCurrentCultureToResponseHeaders = true;
        });
        app.UseRateLimiter();
        app.UseSerilogRequestLogging();
        app.UseAuthentication();
        app.UseAuthorization();
    }
}
