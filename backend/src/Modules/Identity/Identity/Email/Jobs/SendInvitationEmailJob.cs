using Microsoft.EntityFrameworkCore;
using Parley.Identity.Email.Links;
using Parley.Identity.Features.Invitations.Shared;
using Parley.Identity.Persistence;
using Parley.Notifications.Contracts.Email;
using Parley.SharedKernel.BackgroundJobs;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Identity.Email.Jobs;

/// <summary>
/// Creates a fresh invitation token, stores its hash, and emails the link. The token is
/// generated inside the job, so it is never stored in job arguments or job storage.
/// Runs after the invitation commits. Only a pending invitation gets a token and an email;
/// a retry replaces the previous token, so only the newest link works.
/// </summary>
public sealed class SendInvitationEmailJob(
    IMutableTenantContext tenantContext,
    IdentityDbContext dbContext,
    AuthEmailLinks links,
    ITransactionalEmailSender emailSender,
    TimeProvider timeProvider) : TenantBackgroundJob(tenantContext)
{
    private Guid _invitationId;

    private string _culture = string.Empty;

    /// <summary>Entry point enqueued by the scheduler.</summary>
    public Task ExecuteAsync(Guid organizationId, Guid invitationId, string culture, CancellationToken cancellationToken)
    {
        _invitationId = invitationId;
        _culture = culture;
        return ExecuteAsync(organizationId, cancellationToken);
    }

    /// <summary>
    /// The plain-text email carries the name on one line: control characters, including line breaks, are removed.
    /// </summary>
    private static string PlainTextLine(string value) =>
        string.Concat(value.Where(character => !char.IsControl(character)));

    protected override async Task RunAsync(CancellationToken cancellationToken)
    {
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var invitation = await dbContext.Invitations.FirstOrDefaultAsync(row => row.Id == _invitationId, cancellationToken);
        if (invitation is null
            || invitation.AcceptedAt is not null
            || invitation.RevokedAt is not null
            || invitation.ExpiresAt <= nowUtc)
        {
            return;
        }

        var token = InvitationTokens.Create();
        invitation.TokenHash = InvitationTokens.Hash(token);
        var organizationName = await dbContext.Organizations
            .Where(organization => organization.Id == invitation.OrganizationId)
            .Select(organization => organization.Name)
            .SingleAsync(cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var variables = EmailTemplateVariables.From(
            EmailVariableNames.InvitationLink,
            links.AcceptInvitation(token),
            (EmailVariableNames.OrganizationName, PlainTextLine(organizationName)));
        var result = await emailSender.SendAsync(
            invitation.EmailNormalized,
            EmailTemplateKey.Invitation,
            _culture,
            variables,
            cancellationToken);
        SendConfirmationEmailJob.ThrowIfFailed(result, nameof(SendInvitationEmailJob));
    }
}
