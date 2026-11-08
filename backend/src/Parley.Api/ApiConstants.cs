namespace Parley.Api;

/// <summary>
/// Route and document names shared by the API pipeline.
/// </summary>
public static class ApiConstants
{
    public const string HealthRoute = "/health";
    public const string DatabaseHealthCheckName = "database";
    public const string OpenApiDocumentName = "v1";
    public const string ServiceName = "parley-api";
    public const string ProblemCode = "code";
    public const string ProblemTraceId = "traceId";
    public const string ProblemErrors = "errors";
    public const string ProblemFieldCode = "code";
    public const string ProblemFieldMessage = "message";
    public const string ProblemFieldParameters = "params";
    public const string HangfireDashboardRoute = "/hangfire";
    public const string IdempotencyKeyHeader = "Idempotency-Key";
    public const string ConsoleOutputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}";
}
