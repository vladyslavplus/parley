using Mediator;
using Microsoft.AspNetCore.Identity;
using Parley.Identity.Domain.Users;
using Parley.Identity.Email;
using Parley.SharedKernel.Results;

namespace Parley.Identity.Features.Accounts.ForgotPassword;

public sealed class ForgotPasswordCommandHandler(
    UserManager<User> userManager,
    AuthEmailScheduler emailScheduler) : IRequestHandler<ForgotPasswordCommand, Result>
{
    public async ValueTask<Result> Handle(ForgotPasswordCommand command, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(command.Email);
        if (user is { EmailConfirmed: true, LastActiveOrganizationId: { } organizationId })
        {
            emailScheduler.SchedulePasswordResetEmail(organizationId, user.Id, command.Culture);
        }

        return Result.Success();
    }
}
