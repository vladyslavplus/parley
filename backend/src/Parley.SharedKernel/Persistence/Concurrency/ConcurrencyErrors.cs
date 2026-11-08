using Microsoft.EntityFrameworkCore;
using Parley.SharedKernel.Errors;
using Parley.SharedKernel.Results;

namespace Parley.SharedKernel.Persistence.Concurrency;

/// <summary>
/// Single mapping point from a stale <c>xmin</c> save to the shared
/// <see cref="ErrorCodes.ConcurrencyConflict"/> failure. Callers render it
/// through the standard ProblemDetails path; this type only classifies.
/// </summary>
public static class ConcurrencyErrors
{
    public static bool IsConcurrencyConflict(Exception exception) => exception is DbUpdateConcurrencyException;

    public static Error ToConflictError() => Error.Conflict(ErrorCodes.ConcurrencyConflict);
}
