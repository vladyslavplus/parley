using Mediator;
using Microsoft.AspNetCore.Identity;
using Parley.Identity.Contracts.Errors;
using Parley.Identity.Domain.Users;
using Parley.SharedKernel.Errors;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Identity.Features.Profile.UpdateProfile;

public sealed class UpdateProfileCommandHandler(
    UserManager<User> userManager,
    ICurrentUser currentUser) : IRequestHandler<UpdateProfileCommand, Result>
{
    public async ValueTask<Result> Handle(UpdateProfileCommand command, CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Result.Failure(Error.NotFound(IdentityErrorCodes.UserNotFound));
        }

        user.DisplayName = string.IsNullOrWhiteSpace(command.DisplayName) ? null : command.DisplayName.Trim();
        user.Locale = command.Locale;
        user.TimeZone = command.TimeZone;
        user.Theme = command.Theme;

        var updated = await userManager.UpdateAsync(user);
        return updated.Succeeded
            ? Result.Success()
            : Result.Failure(Error.Validation(ErrorCodes.ValidationFailed));
    }
}
