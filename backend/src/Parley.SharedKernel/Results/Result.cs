namespace Parley.SharedKernel.Results;

/// <summary>
/// Minimal result type used to return failures without throwing free-text messages.
/// </summary>
public sealed class Result : IResult<Result>
{
    private Result(bool isSuccess, Error? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public Error? Error { get; }

    public static Result Success() => new(true, null);

    public static Result Failure(Error error) => new(false, error);
}
