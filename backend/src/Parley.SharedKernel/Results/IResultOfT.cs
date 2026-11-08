namespace Parley.SharedKernel.Results;

/// <summary>
/// Result factory contract used by pipeline behaviors.
/// </summary>
public interface IResult<TSelf> : IResult
    where TSelf : IResult<TSelf>
{
    static abstract TSelf Failure(Error error);
}
