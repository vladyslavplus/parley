namespace Parley.SharedKernel.Tenancy.Exceptions;

/// <summary>
/// Thrown when the current user is required but none is available.
/// </summary>
public sealed class UserContextNotSetException : InvalidOperationException
{
    private const string MessageText =
        "A user identifier was required but none is available. " +
        "ICurrentUser must provide a user for the current HTTP request or job scope.";

    public UserContextNotSetException() : base(MessageText)
    {
    }
}
