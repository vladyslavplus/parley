using Mediator;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Parley.Campaigns.Contracts.Errors;
using Parley.Campaigns.Contracts.Events;
using Parley.Campaigns.Domain.Suppressions;
using Parley.Campaigns.Persistence;
using Parley.Campaigns.Sending.Unsubscribe;
using Parley.SharedKernel.Outbox.Publishing;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Campaigns.Features.Recipients.Unsubscribe;

/// <summary>
/// Verifies the token, then suppresses the address of the recipient it names, under the organization the token carries.
/// The suppression and its event commit together. A second request, or a concurrent one, finds the suppression already
/// there and returns success without a second event.
/// </summary>
public sealed class UnsubscribeCommandHandler(
    CampaignsDbContext dbContext,
    IMutableTenantContext tenantContext,
    UnsubscribeTokenCodec tokens,
    IModuleOutbox<CampaignsDbContext> outbox,
    TimeProvider timeProvider) : IRequestHandler<UnsubscribeCommand, Result>
{
    public async ValueTask<Result> Handle(UnsubscribeCommand command, CancellationToken cancellationToken)
    {
        var verification = tokens.Verify(command.Token);
        if (verification.Check == UnsubscribeTokenCheck.Expired)
        {
            return Result.Failure(Error.Validation(CampaignsErrorCodes.UnsubscribeTokenExpired));
        }

        if (verification.Check != UnsubscribeTokenCheck.Valid || verification.Claims is not { } claims)
        {
            return Result.Failure(Error.Validation(CampaignsErrorCodes.UnsubscribeTokenInvalid));
        }

        // No sign-in on this request: the signed token names the organization, and the scope ends with the request.
        tenantContext.Set(claims.OrganizationId);
        try
        {
            return await SuppressAsync(claims, cancellationToken);
        }
        finally
        {
            tenantContext.Clear();
        }
    }

    private async Task<Result> SuppressAsync(UnsubscribeTokenClaims claims, CancellationToken cancellationToken)
    {
        var address = await dbContext.Recipients
            .AsNoTracking()
            .Where(recipient => recipient.CampaignId == claims.CampaignId && recipient.ContactId == claims.ContactId)
            .Select(recipient => recipient.EmailSnapshot)
            .FirstOrDefaultAsync(cancellationToken);
        var normalized = SuppressionAddresses.Normalize(address);
        if (normalized is null)
        {
            return Result.Failure(Error.Validation(CampaignsErrorCodes.UnsubscribeTokenInvalid));
        }

        var alreadySuppressed = await dbContext.Suppressions
            .AsNoTracking()
            .AnyAsync(suppression => suppression.EmailNormalized == normalized, cancellationToken);
        if (alreadySuppressed)
        {
            return Result.Success();
        }

        var now = timeProvider.GetUtcNow();
        dbContext.Suppressions.Add(new Suppression
        {
            OrganizationId = claims.OrganizationId,
            ContactId = claims.ContactId,
            EmailNormalized = normalized,
            Reason = SuppressionReasons.Unsubscribed,
            SuppressedAt = now.UtcDateTime,
        });
        outbox.Add(new RecipientUnsubscribed
        {
            CampaignId = claims.CampaignId,
            ContactId = claims.ContactId,
            SuppressedAt = now,
        });

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // A concurrent request suppressed the address first, and its event was written with its row.
        }

        return Result.Success();
    }
}
