using Hangfire;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Parley.Api;
using Parley.Api.Hangfire;
using Parley.Api.Health;
using Parley.Api.Startup;

var builder = WebApplication.CreateBuilder(args);

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

public partial class Program
{
}
