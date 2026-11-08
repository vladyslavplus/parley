using Microsoft.AspNetCore.Identity;
using Parley.Identity.Domain.Users;
using Parley.Identity.Email.Links;
using Parley.Notifications.Contracts.Email;
using Parley.SharedKernel.BackgroundJobs;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Identity.Email.Jobs;

/// <summary>
/// Sends the password reset link to a confirmed account. Same rules as
/// <see cref="SendConfirmationEmailJob"/>: the token is generated in the job, and
/// unconfirmed or deleted accounts receive nothing.
/// </summary>
public sealed class SendPasswordResetEmailJob(
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
        if (user is null || !user.EmailConfirmed || string.IsNullOrEmpty(user.Email))
        {
            return;
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var variables = EmailTemplateVariables.From(
            EmailVariableNames.ResetLink,
            links.ResetPassword(user.Id, token),
            (EmailVariableNames.DisplayName, user.Email));

        var result = await emailSender.SendAsync(
            user.Email,
            EmailTemplateKey.PasswordReset,
            _culture,
            variables,
            cancellationToken);
        SendConfirmationEmailJob.ThrowIfFailed(result, nameof(SendPasswordResetEmailJob));
    }
}
