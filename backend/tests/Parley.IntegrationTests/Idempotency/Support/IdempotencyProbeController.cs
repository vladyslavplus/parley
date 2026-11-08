using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Parley.Api.Idempotency;
using HeaderNames = Microsoft.Net.Http.Headers.HeaderNames;

namespace Parley.IntegrationTests.Idempotency.Support;

/// <summary>
/// Test-only endpoint exercising the idempotency filter.
/// </summary>
[ApiController]
[Route("test/idempotency")]
public sealed class IdempotencyProbeController(IdempotencyProbeRecorder recorder) : ControllerBase
{
    /// <summary>Non-binary-safe bytes: any text decoding would corrupt them.</summary>
    public static readonly byte[] BinaryPayload = [0x00, 0xFF, 0x10, 0x80, 0xC3, 0x28];

    [HttpPost("status/{status:int}")]
    [IdempotencyKey]
    public IActionResult Status(int status)
    {
        recorder.Increment();
        return StatusCode(status, new { status });
    }

    [HttpPost("resource")]
    [IdempotencyKey]
    public IActionResult Resource()
    {
        recorder.Increment();
        Response.Headers[HeaderNames.Location] = "/test/idempotency/resource/1";
        Response.Headers[HeaderNames.ETag] = "\"v1\"";
        Response.StatusCode = StatusCodes.Status201Created;
        return new FileContentResult(BinaryPayload, "application/octet-stream");
    }

    [HttpPost("increment")]
    [IdempotencyKey]
    public async Task<IActionResult> Increment(CancellationToken cancellationToken)
    {
        // The delay forces overlap for the concurrent-first-use test.
        await Task.Delay(150, cancellationToken);
        var count = recorder.Increment();
        if (recorder.TakeFailure())
        {
            throw new InvalidOperationException("Probe handler failed.");
        }

        return Ok(new { count });
    }
}
