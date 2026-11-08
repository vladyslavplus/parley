using Microsoft.AspNetCore.Mvc;

namespace Parley.Api.Idempotency;

/// <summary>
/// Writes a stored idempotency response back byte-for-byte: status, body
/// bytes, content type and allow-listed headers. Binary bodies stay exact
/// because nothing is decoded as text on replay.
/// </summary>
internal sealed class IdempotencyReplayResult(
    int statusCode,
    byte[] body,
    string? contentType,
    IReadOnlyDictionary<string, string> headers) : IActionResult
{
    public async Task ExecuteResultAsync(ActionContext actionContext)
    {
        var response = actionContext.HttpContext.Response;
        response.StatusCode = statusCode;
        response.ContentType = contentType;
        foreach (var (name, value) in headers)
        {
            response.Headers[name] = value;
        }

        if (body.Length > 0)
        {
            await response.Body.WriteAsync(body, actionContext.HttpContext.RequestAborted);
        }
    }
}
