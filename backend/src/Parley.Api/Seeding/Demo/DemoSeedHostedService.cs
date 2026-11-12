namespace Parley.Api.Seeding.Demo;

/// <summary>
/// Runs the Development demo seed in the background after the host starts, so health checks answer while the workspace is
/// generated. The runner logs every step; this service only makes sure a failure is logged and never stops the host.
/// </summary>
public sealed class DemoSeedHostedService(DemoSeedRunner runner, ILogger<DemoSeedHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Never delay the web server: the background work starts on the first yield.
        await Task.Yield();
        try
        {
            var outcome = await runner.RunAsync(stoppingToken);
            logger.LogInformation("Demo seed finished with {Status} in {Duration}.", outcome.Status, outcome.Duration);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // The host is shutting down; the marker stays, so the next start resumes.
        }
        catch (Exception exception)
        {
            logger.LogError("Demo seed stopped with {ExceptionType}; the API keeps running.", exception.GetType().Name);
        }
    }
}
