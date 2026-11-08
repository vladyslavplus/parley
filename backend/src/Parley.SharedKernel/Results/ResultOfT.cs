namespace Parley.SharedKernel.Results;

/// <summary>
/// Minimal generic result type. A successful result carries a value;
/// a failed result carries an <see cref="Error"/>.
/// </summary>
public sealed class Result<TValue> : IResult<Result<TValue>>
{
    private Result(bool isSuccess, TValue? value, Error? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public bool IsSuccess { get; }

    public TValue? Value { get; }

    public Error? Error { get; }

    public static Result<TValue> Success(TValue value) => new(true, value, null);

    public static Result<TValue> Failure(Error error) => new(false, default, error);
}
