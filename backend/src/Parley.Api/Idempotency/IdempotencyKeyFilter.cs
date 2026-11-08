using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Parley.Api.Localization;
using Parley.SharedKernel.Errors;
using Parley.SharedKernel.Idempotency;
using Parley.SharedKernel.Idempotency.Abstractions;
using Parley.SharedKernel.Idempotency.Persistence;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Api.Idempotency;

/// <summary>
/// Implements the module attributes such as <see cref="CrmIdempotencyKeyAttribute"/>; resolved per request
/// from DI by the attribute. Registered as a scoped service by the host
/// together with the module's <see cref="IIdempotencyStore"/>.
/// </summary>
public sealed class IdempotencyKeyFilter<TDbContext>(
    IModuleIdempotencyStore<TDbContext> store,
    IOptions<IdempotencyOptions> options,
    TimeProvider timeProvider,
    ICurrentUser currentUser,
    ErrorTextLocalizer errorTexts,
    ILogger<IdempotencyKeyFilter<TDbContext>> logger) : IAsyncActionFilter
    where TDbContext : DbContext
{
    private const int BodyReadChunkSize = 8192;

    private static readonly byte[] HashSeparator = [0];

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext;
        if (!httpContext.Request.Headers.TryGetValue(ApiConstants.IdempotencyKeyHeader, out var keyValue)
            || string.IsNullOrWhiteSpace(keyValue.ToString()))
        {
            await next();
            return;
        }

        var key = keyValue.ToString();
        if (key.Length > IdempotencyOptions.MaximumKeyLength)
        {
            context.Result = BadRequest(Error.Validation(ErrorCodes.IdempotencyKeyTooLong), httpContext);
            return;
        }

        var request = httpContext.Request;
        request.EnableBuffering();
        var cancellationToken = httpContext.RequestAborted;
        var requestHash = await ComputeRequestHashAsync(request, cancellationToken);
        if (requestHash is null)
        {
            context.Result = BadRequest(Error.Validation(ErrorCodes.IdempotencyBodyTooLarge), httpContext);
            return;
        }

        var now = DateTime.SpecifyKind(timeProvider.GetUtcNow().UtcDateTime, DateTimeKind.Utc);
        var expiresAt = now.Add(options.Value.KeyLifetime);

        var claimId = await ClaimOrReplayAsync(context, key, requestHash, expiresAt, now, httpContext);
        if (claimId is null)
        {
            return;
        }

        // The action runs first; only afterwards is the result replaced by
        // the capturing wrapper. Setting context.Result before calling next
        // would forbid invoking the delegate, so capture follows execution.
        // An exception thrown by the handler is reported on the executed
        // context (MVC does not rethrow it from next), so the claim is
        // released when executed.Exception is set as well as when next
        // itself throws.
        ActionExecutedContext executed;
        try
        {
            executed = await next();
        }
        catch
        {
            // The handler failed: release the claim so the key stays
            // reusable instead of being poisoned until expiry.
            await TryDeleteClaimAsync(claimId.Value, cancellationToken);
            throw;
        }

        if (executed.Exception is not null)
        {
            await TryDeleteClaimAsync(claimId.Value, cancellationToken);
            return;
        }

        if (executed.Result is null)
        {
            await TryDeleteClaimAsync(claimId.Value, cancellationToken);
            return;
        }

        // Post-action results are replaced on the executed context returned
        // by next(); ActionExecutingContext.Result only short-circuits.
        executed.Result = new IdempotencyCaptureResult(executed.Result, store, claimId.Value, logger);
    }

    private async Task<Guid?> ClaimOrReplayAsync(
        ActionExecutingContext context,
        string key,
        byte[] requestHash,
        DateTime expiresAt,
        DateTime now,
        HttpContext httpContext)
    {
        var cancellationToken = httpContext.RequestAborted;
        while (true)
        {
            var existing = await store.FindAsync(key, cancellationToken);
            existing = await ClearExpiredAsync(existing, now, cancellationToken);
            if (existing is not null)
            {
                if (!requestHash.SequenceEqual(existing.RequestHash))
                {
                    context.Result = Conflict(Error.Conflict(ErrorCodes.IdempotencyKeyReused), httpContext);
                    return null;
                }

                if (existing.ResponseStatus != IdempotencyTableNames.ClaimedStatus)
                {
                    context.Result = Replay(existing);
                    return null;
                }

                // Another request claimed the key and is still running: poll
                // for the stored response instead of running the handler
                // twice. A released claim loops back to claim instead of
                // running without ownership.
                var waited = await WaitForStoredResponseAsync(key, requestHash, httpContext);
                if (waited is not null)
                {
                    context.Result = waited;
                    return null;
                }

                continue;
            }

            var newClaimId = await store.TryClaimAsync(key, requestHash, expiresAt, cancellationToken);
            if (newClaimId.HasValue)
            {
                return newClaimId.Value;
            }

            var waitedAfterRace = await WaitForStoredResponseAsync(key, requestHash, httpContext);
            if (waitedAfterRace is not null)
            {
                context.Result = waitedAfterRace;
                return null;
            }

            // The owner released the key: loop back and claim instead of
            // running the handler without owning the claim.
        }
    }

    private async Task<IdempotencyKey?> ClearExpiredAsync(
        IdempotencyKey? existing,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        if (existing is null || existing.ExpiresAt > nowUtc)
        {
            return existing;
        }

        await store.TryDeleteExpiredClaimAsync(existing.Id, nowUtc, cancellationToken);
        return null;
    }

    private async Task TryDeleteClaimAsync(Guid claimId, CancellationToken cancellationToken)
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

    private async Task<IActionResult?> WaitForStoredResponseAsync(
        string key,
        byte[] requestHash,
        HttpContext httpContext)
    {
        var pollInterval = options.Value.InProgressPollInterval;
        var timeout = options.Value.InProgressWaitTimeout;
        // Real-time polling: integration tests drive the fake clock by hand,
        // so a TimeProvider-backed delay would freeze here.
        var waited = TimeSpan.Zero;
        while (waited < timeout)
        {
            await Task.Delay(pollInterval, httpContext.RequestAborted);
            waited += pollInterval;

            var existing = await store.FindAsync(key, httpContext.RequestAborted);
            if (existing is null)
            {
                return null;
            }

            if (!requestHash.SequenceEqual(existing.RequestHash))
            {
                return Conflict(Error.Conflict(ErrorCodes.IdempotencyKeyReused), httpContext);
            }

            if (existing.ResponseStatus != IdempotencyTableNames.ClaimedStatus)
            {
                return Replay(existing);
            }
        }

        logger.LogInformation(
            "Idempotency key wait timed out for key '{Key}'. Returning a conflict.",
            key);
        return Conflict(Error.Conflict(ErrorCodes.IdempotencyInProgress), httpContext);
    }

    private async Task<byte[]?> ComputeRequestHashAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        var maxBytes = options.Value.MaxRequestBodyBytes;
        if (request.ContentLength.HasValue && request.ContentLength.Value > maxBytes)
        {
            return null;
        }

        using var incremental = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        AppendHashPart(incremental, request.Method);
        AppendHashPart(incremental, request.Path.Value ?? string.Empty);
        AppendHashPart(incremental, request.QueryString.Value ?? string.Empty);
        AppendHashPart(incremental, currentUser.UserId?.ToString() ?? string.Empty);

        var chunk = new byte[BodyReadChunkSize];
        long total = 0;
        int read;
        while ((read = await request.Body.ReadAsync(chunk, cancellationToken)) > 0)
        {
            total += read;
            if (total > maxBytes)
            {
                return null;
            }

            incremental.AppendData(chunk, 0, read);
        }

        request.Body.Position = 0;
        return incremental.GetHashAndReset();
    }

    private static void AppendHashPart(IncrementalHash incremental, string part)
    {
        incremental.AppendData(Encoding.UTF8.GetBytes(part));
        incremental.AppendData(HashSeparator);
    }

    private static IActionResult Replay(IdempotencyKey entry)
    {
        var headers = DeserializeResponseHeaders(entry.ResponseHeaders);
        return new IdempotencyReplayResult(
            entry.ResponseStatus,
            entry.ResponseBody ?? [],
            entry.ResponseContentType,
            headers);
    }

    private static IReadOnlyDictionary<string, string> DeserializeResponseHeaders(string? serialized)
    {
        if (string.IsNullOrWhiteSpace(serialized))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            var headers = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(serialized);
            return headers ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        catch (System.Text.Json.JsonException)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private ObjectResult Conflict(Error error, HttpContext httpContext) =>
        Problem(error, httpContext, StatusCodes.Status409Conflict);

    private ObjectResult BadRequest(Error error, HttpContext httpContext) =>
        Problem(error, httpContext, StatusCodes.Status400BadRequest);

    private ObjectResult Problem(Error error, HttpContext httpContext, int statusCode)
    {
        var detail = errorTexts.Get(error);
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = detail,
            Detail = detail,
            Extensions =
            {
                [ApiConstants.ProblemCode] = error.Code,
                [ApiConstants.ProblemTraceId] = httpContext.TraceIdentifier,
            },
        };
        return new ObjectResult(problemDetails) { StatusCode = statusCode };
    }
}
