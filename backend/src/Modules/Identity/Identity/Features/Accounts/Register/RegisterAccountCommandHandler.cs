using Mediator;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Parley.Identity.Authorization.Seed;
using Parley.Identity.Contracts.Errors;
using Parley.Identity.Contracts.Events;
using Parley.Identity.Domain.Memberships;
using Parley.Identity.Domain.Organizations;
using Parley.Identity.Domain.Users;
using Parley.Identity.Email;
using Parley.Identity.Features.Accounts.Shared;
using Parley.Identity.Persistence;
using Parley.SharedKernel.Outbox.Publishing;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Identity.Features.Accounts.Register;

public sealed class RegisterAccountCommandHandler(
    IdentityDbContext dbContext,
    UserManager<User> userManager,
    AuthEmailScheduler emailScheduler,
    IMutableTenantContext tenantContext,
    IOutbox outbox,
    TimeProvider timeProvider) : IRequestHandler<RegisterAccountCommand, Result>
{
    public async ValueTask<Result> Handle(RegisterAccountCommand command, CancellationToken cancellationToken)
    {
        if (await userManager.FindByEmailAsync(command.Email) is not null)
        {
            return Result.Failure(Error.Conflict(IdentityErrorCodes.UserEmailTaken));
        }

        var strategy = dbContext.Database.CreateExecutionStrategy();
        var created = await strategy.ExecuteAsync(
            async ct => await CreateAccountAsync(command, ct),
            cancellationToken);
        if (!created.IsSuccess)
        {
            return Result.Failure(created.Error!);
        }

        // Only after the commit: the email job reads the committed account.
        emailScheduler.ScheduleConfirmationEmail(created.Value!.OrganizationId, created.Value.UserId, command.Culture);
        return Result.Success();
    }

    private async Task<Result<CreatedAccount>> CreateAccountAsync(RegisterAccountCommand command, CancellationToken cancellationToken)
    {
        var nowUtc = timeProvider.GetUtcNow();
        var organization = new Organization
        {
            Id = Guid.CreateVersion7(),
            Name = command.OrganizationName.Trim(),
            Slug = OrganizationDefaults.NewSlug(),
            TimeZone = OrganizationDefaults.TimeZone,
            DefaultLocale = command.Culture,
            Currency = OrganizationDefaults.Currency,
            Status = OrganizationStatuses.Active,
            CreatedAt = nowUtc.UtcDateTime,
            UpdatedAt = nowUtc.UtcDateTime,
        };

        // The organization is created by this request, so the tenant rows written below
        // (membership, outbox) run under it. The scope ends before the request continues.
        tenantContext.Set(organization.Id);
        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            dbContext.Organizations.Add(organization);

            var user = new User
            {
                Id = Guid.CreateVersion7(),
                UserName = command.Email,
                Email = command.Email,
                Locale = command.Culture,
                LastActiveOrganizationId = organization.Id,
            };

            IdentityResult result;
            try
            {
                result = await userManager.CreateAsync(user, command.Password);
            }
            catch (DbUpdateException exception) when (UserCreationErrors.IsDuplicateAccount(exception))
            {
                // A parallel registration of the same address won the unique index.
                await transaction.RollbackAsync(cancellationToken);
                return Result<CreatedAccount>.Failure(Error.Conflict(IdentityErrorCodes.UserEmailTaken));
            }

            if (!result.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result<CreatedAccount>.Failure(UserCreationErrors.Map(result));
            }

            dbContext.Memberships.Add(new Membership(
                organization.Id,
                user.Id,
                SystemRoleSeed.AdminRoleId,
                teamId: null,
                joinedAt: nowUtc.UtcDateTime));
            outbox.Add(new OrganizationCreated
            {
                EventId = Guid.CreateVersion7(),
                OccurredAt = nowUtc,
                OrganizationId = organization.Id,
            });
            await dbContext.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return Result<CreatedAccount>.Success(new CreatedAccount(organization.Id, user.Id));
        }
        finally
        {
            tenantContext.Clear();
        }
    }

    private sealed record CreatedAccount(Guid OrganizationId, Guid UserId);
}
