using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Parley.Identity.Contracts.Errors;
using Parley.Identity.Persistence;
using Parley.SharedKernel.Persistence.Concurrency;
using Parley.SharedKernel.Results;

namespace Parley.Identity.Features.Accounts.Shared;

/// <summary>
/// Maps a failed account creation to the shared error codes. A duplicate address is a
/// conflict; any other rule failure is reported as an invalid password.
/// </summary>
public static class UserCreationErrors
{
    /// <summary>
    /// True when a save lost a race to another registration of the same address or user name.
    /// The unique index is the final guard; the handler reports it as the duplicate-address conflict.
    /// </summary>
    public static bool IsDuplicateAccount(DbUpdateException exception) =>
        UniqueConstraintViolations.IsViolationOf(exception, IdentityDbContext.UserEmailIndex)
        || UniqueConstraintViolations.IsViolationOf(exception, IdentityDbContext.UserNameIndex);

    public static Error Map(IdentityResult result)
    {
        var duplicate = result.Errors.Any(error =>
            error.Code is nameof(IdentityErrorDescriber.DuplicateEmail) or nameof(IdentityErrorDescriber.DuplicateUserName));
        return duplicate
            ? Error.Conflict(IdentityErrorCodes.UserEmailTaken)
            : Error.Validation(IdentityErrorCodes.UserPasswordInvalid);
    }
}
