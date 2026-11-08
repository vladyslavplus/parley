using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Parley.Api.Localization;
using Parley.SharedKernel.Errors;
using Parley.SharedKernel.Persistence.Concurrency;
using Parley.SharedKernel.Results;

namespace Parley.Api.ErrorHandling;

/// <summary>
/// Single mapping point for unhandled exceptions. A stale <c>xmin</c> save is
/// classified by <see cref="ConcurrencyErrors"/> and rendered as 409 with the
/// shared <see cref="ErrorCodes.ConcurrencyConflict"/> code through the same
/// ProblemDetails shape as controller results. Anything else is a generic 500
/// without internals.
/// </summary>
public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    ErrorTextLocalizer errorTexts,
    IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // The exception object goes to the logger with its stack; the Serilog
        // console sink masks personal data in the rendered exception chain.
        logger.LogError(exception, "Unhandled exception.");

        var isConcurrencyConflict = ConcurrencyErrors.IsConcurrencyConflict(exception);
        var error = isConcurrencyConflict
            ? ConcurrencyErrors.ToConflictError()
            : Error.Failure(ErrorCodes.Unexpected);
        var detail = errorTexts.Get(error);
        var statusCode = isConcurrencyConflict
            ? StatusCodes.Status409Conflict
            : StatusCodes.Status500InternalServerError;
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

        httpContext.Response.StatusCode = statusCode;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
        });
    }
}
