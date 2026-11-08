using Mediator;
using Microsoft.AspNetCore.Identity;
using Parley.Identity.Domain.Users;
using Parley.Identity.Email;
using Parley.SharedKernel.Results;

namespace Parley.Identity.Features.Accounts.ResendConfirmation;

public sealed class ResendConfirmationCommandHandler(
    UserManager<User> userManager,
    AuthEmailScheduler emailScheduler) : IRequestHandler<ResendConfirmationCommand, Result>
{
    public async ValueTask<Result> Handle(ResendConfirmationCommand command, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(command.Email);
        if (user is { EmailConfirmed: false, LastActiveOrganizationId: { } organizationId })
        {
            emailScheduler.ScheduleConfirmationEmail(organizationId, user.Id, command.Culture);
        }

        return Result.Success();
    }
}
