using System.Net.Mime;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Parley.Api.Health;

/// <summary>
/// Writes health results as a small JSON document.
/// </summary>
public static class HealthResponseWriter
{
    public static Task WriteResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = MediaTypeNames.Application.Json;
        var payload = new
        {
            status = report.Status.ToString().ToLowerInvariant(),
            checks = report.Entries
                .Select(entry => new { name = entry.Key, status = entry.Value.Status.ToString().ToLowerInvariant() })
                .ToArray(),
        };
        return context.Response.WriteAsJsonAsync(payload, context.RequestAborted);
    }
}
