using Microsoft.AspNetCore.Identity;
using Parley.Identity.Domain.Users;
using Parley.Identity.Email.Links;
using Parley.Notifications.Contracts.Email;
using Parley.SharedKernel.BackgroundJobs;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Identity.Email.Jobs;

/// <summary>
/// Sends the email confirmation link. The token is generated inside the job, not
/// passed as an argument, so it is never stored in job storage. Retries generate a
/// new token and the old one stays valid until used.
/// </summary>
/// <remarks>
/// Idempotent by construction: a repeated run sends a duplicate link at worst, and
/// a link for an account that is already confirmed is not sent at all.
/// </remarks>
public sealed class SendConfirmationEmailJob(
    IMutableTenantContext tenantContext,
    UserManager<User> userManager,
    AuthEmailLinks links,
    ITransactionalEmailSender emailSender) : TenantBackgroundJob(tenantContext)
{
    private Guid _userId;

    private string _culture = string.Empty;

    /// <summary>Entry point enqueued by the scheduler.</summary>
    public Task ExecuteAsync(Guid organizationId, Guid userId, string culture, CancellationToken cancellationToken)
    {
        _userId = userId;
        _culture = culture;
        return ExecuteAsync(organizationId, cancellationToken);
    }

    protected override async Task RunAsync(CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(_userId.ToString());
        if (user is null || user.EmailConfirmed || string.IsNullOrEmpty(user.Email))
        {
            return;
        }

        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var variables = EmailTemplateVariables.From(
            EmailVariableNames.ConfirmationLink,
            links.ConfirmEmail(user.Id, token),
            (EmailVariableNames.DisplayName, user.Email));

        var result = await emailSender.SendAsync(
            user.Email,
            EmailTemplateKey.EmailConfirmation,
            _culture,
            variables,
            cancellationToken);
        ThrowIfFailed(result, nameof(SendConfirmationEmailJob));
    }

    internal static void ThrowIfFailed(Result result, string jobName)
    {
        if (!result.IsSuccess)
        {
            // The error code is stable and carries no recipient, subject or link.
            throw new InvalidOperationException($"{jobName} could not send mail: {result.Error?.Code}");
        }
    }
}
