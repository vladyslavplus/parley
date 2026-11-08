using Mediator;
using Microsoft.AspNetCore.Identity;
using Parley.Identity.Authentication.Refresh;
using Parley.Identity.Contracts.Errors;
using Parley.Identity.Domain.Users;
using Parley.SharedKernel.Results;

namespace Parley.Identity.Features.Accounts.ResetPassword;

public sealed class ResetPasswordCommandHandler(
    UserManager<User> userManager,
    RefreshTokenService refreshTokens) : IRequestHandler<ResetPasswordCommand, Result>
{
    public async ValueTask<Result> Handle(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(command.UserId.ToString());
        if (user is null)
        {
            return Result.Failure(Error.NotFound(IdentityErrorCodes.UserNotFound));
        }

        var reset = await userManager.ResetPasswordAsync(user, command.Token, command.NewPassword);
        if (!reset.Succeeded)
        {
            var tokenInvalid = reset.Errors.Any(error => error.Code == nameof(IdentityErrorDescriber.InvalidToken));
            return Result.Failure(Error.Validation(tokenInvalid
                ? IdentityErrorCodes.AuthTokenInvalid
                : IdentityErrorCodes.UserPasswordInvalid));
        }

        await refreshTokens.RevokeAllForUserAsync(user.Id, cancellationToken);
        return Result.Success();
    }
}
