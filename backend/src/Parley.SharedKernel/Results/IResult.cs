namespace Parley.SharedKernel.Results;

/// <summary>
/// Common view of a result for pipeline behaviors.
/// </summary>
public interface IResult
{
    bool IsSuccess { get; }

    Error? Error { get; }
}
