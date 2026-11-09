using Mediator;
using Microsoft.AspNetCore.Identity;
using Parley.Identity.Authentication.Passwords;
using Parley.Identity.Authentication.Refresh;
using Parley.Identity.Authentication.Tokens;
using Parley.Identity.Authorization.Permissions;
using Parley.Identity.Contracts.Audit;
using Parley.Identity.Contracts.Errors;
using Parley.Identity.Domain.Users;
using Parley.Identity.Persistence;
using Parley.SharedKernel.Outbox.Audit;
using Parley.SharedKernel.Outbox.Publishing;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Identity.Features.Sessions.SignIn;

public sealed class SignInCommandHandler(
    IdentityDbContext dbContext,
    UserManager<User> userManager,
    RefreshTokenService refreshTokens,
    DummyPasswordHash dummyPasswordHash,
    IMutableTenantContext tenantContext,
    IModuleOutbox<IdentityDbContext> outbox,
    TimeProvider timeProvider) : IRequestHandler<SignInCommand, Result<AuthTokens>>
{
    public async ValueTask<Result<AuthTokens>> Handle(SignInCommand command, CancellationToken cancellationToken)
    {
        var invalid = Result<AuthTokens>.Failure(Error.Unauthorized(IdentityErrorCodes.AuthInvalidCredentials));

        var user = await userManager.FindByEmailAsync(command.Email);
        if (user is null)
        {
            dummyPasswordHash.Consume(command.Password);
            return invalid;
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            dummyPasswordHash.Consume(command.Password);
            return invalid;
        }

        if (!await userManager.CheckPasswordAsync(user, command.Password))
        {
            await userManager.AccessFailedAsync(user);
            await RecordFailedAttemptAsync(user, lockedNow: await userManager.IsLockedOutAsync(user), cancellationToken);
            return invalid;
        }

        await userManager.ResetAccessFailedCountAsync(user);

        if (!user.EmailConfirmed)
        {
            return Result<AuthTokens>.Failure(Error.Forbidden(IdentityErrorCodes.AuthEmailNotConfirmed));
        }

        // A deactivated member gets the generic refusal, the same as a wrong password.
        var organizationId = await ActiveMembershipQueries.ResolveSignInOrganizationAsync(
            dbContext,
            user.Id,
            user.LastActiveOrganizationId,
            cancellationToken);
        if (organizationId is not { } activeOrganizationId)
        {
            return invalid;
        }

        var tokens = await refreshTokens.IssueNewFamilyAsync(user.Id, activeOrganizationId, user.IsPlatformOperator, cancellationToken);
        return Result<AuthTokens>.Success(tokens);
    }

    /// <summary>
    /// Audits a wrong password of a known account, and the lockout when this failure locked it.
    /// The row is scoped to the account's last active organization; an account without one is not audited.
    /// The response stays the same, so the audit write is invisible to the caller.
    /// </summary>
    private async Task RecordFailedAttemptAsync(User user, bool lockedNow, CancellationToken cancellationToken)
    {
        if (user.LastActiveOrganizationId is not { } organizationId)
        {
            return;
        }

        // The request is anonymous, so the tenant scope is set for the audit write only.
        tenantContext.Set(organizationId);
        try
        {
            outbox.AddAuditRecorded(user.Id, organizationId, AuditActions.SignInFailed, timeProvider, AuditTargetTypes.User, user.Id);
            if (lockedNow)
            {
                outbox.AddAuditRecorded(user.Id, organizationId, AuditActions.AccountLocked, timeProvider, AuditTargetTypes.User, user.Id);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            tenantContext.Clear();
        }
    }
}
