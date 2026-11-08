using Mediator;
using Microsoft.AspNetCore.Identity;
using Parley.Identity.Contracts.Errors;
using Parley.Identity.Domain.Users;
using Parley.SharedKernel.Results;

namespace Parley.Identity.Features.Accounts.ConfirmEmail;

public sealed class ConfirmEmailCommandHandler(UserManager<User> userManager) : IRequestHandler<ConfirmEmailCommand, Result>
{
    public async ValueTask<Result> Handle(ConfirmEmailCommand command, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(command.UserId.ToString());
        if (user is null)
        {
            return Result.Failure(Error.NotFound(IdentityErrorCodes.UserNotFound));
        }

        var confirmed = await userManager.ConfirmEmailAsync(user, command.Token);
        if (!confirmed.Succeeded)
        {
            return Result.Failure(Error.Validation(IdentityErrorCodes.AuthTokenInvalid));
        }

        // Rotates the stamp that the confirmation token embeds, so the link works once.
        await userManager.UpdateSecurityStampAsync(user);
        return Result.Success();
    }
}
