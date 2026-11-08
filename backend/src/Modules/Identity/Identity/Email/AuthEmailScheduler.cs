using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.Extensions.Options;
using Parley.Identity.Email.Jobs;
using Parley.Identity.Options.Auth;

namespace Parley.Identity.Email;

/// <summary>
/// Queues account email jobs. Called only after the database change commits, so no
/// email is sent for a change that rolled back, and no external I/O happens inside
/// a transaction.
/// </summary>
/// <remarks>
/// The enqueue is not part of the business transaction. If the process stops
/// between commit and enqueue, the email is lost; the user can ask for it again
/// through resend-confirmation or forgot-password. Lost mail is acceptable here,
/// duplicate mail is not, so the job itself only sends once per run.
/// </remarks>
public sealed class AuthEmailScheduler(IBackgroundJobClient jobs, IOptions<AuthOptions> options)
{
    private readonly string _queue = options.Value.EmailQueueName;

    public void ScheduleConfirmationEmail(Guid organizationId, Guid userId, string culture)
    {
        var job = Job.FromExpression<SendConfirmationEmailJob>(
            job => job.ExecuteAsync(organizationId, userId, culture, CancellationToken.None));
        jobs.Create(job, new EnqueuedState(_queue));
    }

    public void SchedulePasswordResetEmail(Guid organizationId, Guid userId, string culture)
    {
        var job = Job.FromExpression<SendPasswordResetEmailJob>(
            job => job.ExecuteAsync(organizationId, userId, culture, CancellationToken.None));
        jobs.Create(job, new EnqueuedState(_queue));
    }

    public void ScheduleInvitationEmail(Guid organizationId, Guid invitationId, string culture)
    {
        var job = Job.FromExpression<SendInvitationEmailJob>(
            job => job.ExecuteAsync(organizationId, invitationId, culture, CancellationToken.None));
        jobs.Create(job, new EnqueuedState(_queue));
    }
}
