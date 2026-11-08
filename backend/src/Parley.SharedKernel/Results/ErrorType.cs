namespace Parley.SharedKernel.Results;

/// <summary>
/// Error category used to map failures to HTTP status codes.
/// </summary>
public enum ErrorType
{
    Validation,
    Unauthorized,
    Forbidden,
    NotFound,
    Conflict,
    Failure,
}
