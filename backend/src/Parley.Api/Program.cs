using Hangfire;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Parley.Api;
using Parley.Api.Hangfire;
using Parley.Api.Health;
using Parley.Api.Seeding.Demo;
using Parley.Api.Startup;

// The content root is the folder of the assembly, so appsettings.json beside the published API loads from any working directory.
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
});

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
builder.Services.AddBackgroundProcessing();
builder.Services.AddMediatorPipeline();
builder.Services.AddHealthEndpoints();

builder.Services.AddHostedService<PendingMigrationsGuard>();
builder.Services.AddDemoSeed(builder.Configuration);

var app = builder.Build();

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
