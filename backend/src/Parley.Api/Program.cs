using Hangfire;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Parley.Api;
using Parley.Api.Cli.SeedDemo;
using Parley.Api.Hangfire;
using Parley.Api.Health;
using Parley.Api.Startup;

var builder = WebApplication.CreateBuilder(args);

// The seed verb uses the full host for its services, but never serves traffic on a fixed port and never runs jobs.
var isSeedDemo = args.Length > 0 && args[0] == SeedDemoCommandConstants.Verb;
if (isSeedDemo)
{
    builder.WebHost.UseUrls(SeedDemoCommandConstants.LoopbackAnyPortUrl);

    // A failed host start is reported by the seed command as one coded line; the host's own log would repeat it with a stack trace.
    builder.Logging.AddFilter("Microsoft.Extensions.Hosting", LogLevel.None);
}

builder.ConfigureSerilog();
builder.Services.AddPlatformOptions(builder.Configuration);
builder.Services.AddIdempotency(builder.Configuration);
builder.Services.AddPersistence();
builder.Services.AddObservability();
builder.Services.AddApiPipeline();
builder.Services.AddTransactionsAndTenancy();
builder.Services.AddModules(builder.Configuration);
builder.Services.AddRateLimiting();
builder.Services.AddAuthenticationAndAuthorization();
builder.Services.AddBackgroundProcessing(runJobs: !isSeedDemo);
builder.Services.AddMediatorPipeline();
builder.Services.AddHealthEndpoints();

var app = builder.Build();

if (args.Length > 0 && args[0] == SeedDemoCommandConstants.Verb)
{
    return await SeedDemoCommand.RunAsync(app, args[1..], app.Lifetime.ApplicationStopping);
}

app.UseRequestPipeline();
app.MapDevelopmentEndpoints();

app.MapHangfireDashboard(ApiConstants.HangfireDashboardRoute, new DashboardOptions
{
    Authorization = [app.Services.GetRequiredService<PlatformOperatorDashboardAuthorizationFilter>()],
});
app.MapControllers();
app.MapHealthChecks(ApiConstants.HealthRoute, new HealthCheckOptions { ResponseWriter = HealthResponseWriter.WriteResponse });

app.Run();

return 0;

public partial class Program
{
}
