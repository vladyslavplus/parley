using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Parley.SharedKernel.Idempotency.Abstractions;

namespace Parley.Api.Idempotency;

/// <summary>
/// Executes the produced action result into a buffer, stores the status, body
/// bytes and allow-listed headers on the idempotency claim, then writes the
/// buffered bytes to the response. Only replayable responses are stored
/// (2xx and 4xx except 401, 403, 409 and 429): a transient failure must never poison the key
/// for the full lifetime. If storing fails, the claim is deleted so the key
/// stays reusable and the response is still delivered (losing the replay is
/// better than failing the request) and the store error is logged.
/// </summary>
internal sealed class IdempotencyCaptureResult(
    IActionResult innerResult,
    IIdempotencyStore store,
    Guid claimId,
    ILogger<IdempotencyKeyFilter> logger) : IActionResult
{
    public async Task ExecuteResultAsync(ActionContext actionContext)
    {
        var response = actionContext.HttpContext.Response;
        var originalBody = response.Body;
        using var buffer = new MemoryStream();
        response.Body = buffer;
        try
        {
            await innerResult.ExecuteResultAsync(actionContext);
        }
        catch
        {
            response.Body = originalBody;
            await TryDeleteClaimAsync(actionContext.HttpContext.RequestAborted);
            throw;
        }

        response.Body = originalBody;
        var bodyBytes = buffer.ToArray();
        var status = response.StatusCode;
        if (!IsStorableStatus(status))
        {
            await TryDeleteClaimAsync(actionContext.HttpContext.RequestAborted);
            await originalBody.WriteAsync(bodyBytes, actionContext.HttpContext.RequestAborted);
            return;
        }

        try
        {
            await store.CompleteClaimAsync(
                claimId,
                status,
                bodyBytes,
                response.ContentType,
                SerializeResponseHeaders(response),
                actionContext.HttpContext.RequestAborted);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Failed to store the idempotency response for claim '{ClaimId}'.",
                claimId);
            await TryDeleteClaimAsync(actionContext.HttpContext.RequestAborted);
        }

        await originalBody.WriteAsync(bodyBytes, actionContext.HttpContext.RequestAborted);
    }

    /// <summary>
    /// Statuses that describe the request itself. Authentication, permission,
    /// conflict and rate-limit answers describe the caller's state at one moment,
    /// so they are never replayed.
    /// </summary>
    private static bool IsStorableStatus(int status) =>
        status is >= StatusCodes.Status200OK and < StatusCodes.Status300MultipleChoices or >= StatusCodes.Status400BadRequest and < StatusCodes.Status500InternalServerError and not (StatusCodes.Status401Unauthorized
            or StatusCodes.Status403Forbidden
            or StatusCodes.Status409Conflict
            or StatusCodes.Status429TooManyRequests);

    private static string? SerializeResponseHeaders(HttpResponse response)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in IdempotencyKeyFilter.AllowedResponseHeaderNames)
        {
            if (response.Headers.TryGetValue(name, out var values))
            {
                headers[name] = values.ToString();
            }
        }

        return headers.Count > 0 ? JsonSerializer.Serialize(headers) : null;
    }

    private async Task TryDeleteClaimAsync(CancellationToken cancellationToken)
    {
        try
        {
            await store.DeleteClaimAsync(claimId, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Failed to release the idempotency claim '{ClaimId}'.",
                claimId);
        }
    }
}
