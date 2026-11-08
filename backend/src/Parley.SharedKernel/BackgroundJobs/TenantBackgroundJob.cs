using Parley.SharedKernel.Observability;
using Parley.SharedKernel.Tenancy.Abstractions;
using Parley.SharedKernel.Tenancy.Implementations;

namespace Parley.SharedKernel.BackgroundJobs;

/// <summary>
/// Base class for Hangfire jobs. The organization id travels in the job
/// arguments; the base sets <see cref="JobTenantContext"/> for the execution
/// flow and clears it afterwards, even when the job throws, so parallel jobs
/// never leak one tenant into another. A job whose arguments carry no
/// organization id fails loudly unless it is explicitly declared platform-wide
/// (<see cref="PlatformWide"/>).
///
/// Jobs remain idempotent by construction (see the backend decisions): the
/// caller enqueues them with the organization id and the base class is the
/// only place that restores tenant state.
/// </summary>
public abstract class TenantBackgroundJob(IMutableTenantContext tenantContext)
{
    private const string ActivityName = "background.job";

    /// <summary>
    /// The tenant bound to this job scope; the ambient organization is set by
    /// the base class for the duration of <see cref="RunAsync"/>.
    /// </summary>
    protected IMutableTenantContext TenantContext => tenantContext;

    /// <summary>
    /// Set to <c>true</c> to declare the job platform-wide: it runs without an
    /// organization id, e.g. sweeps that enumerate organizations and then open
    /// one scope per organization.
    /// </summary>
    protected virtual bool PlatformWide => false;

    /// <summary>
    /// Entry point called by the background worker. Enqueue sites pass
    /// <see cref="CancellationToken.None"/> as the placeholder: Hangfire replaces
    /// the token with its own shutdown token just before the job runs.
    /// </summary>
    public async Task ExecuteAsync(Guid? organizationId, CancellationToken cancellationToken)
    {
        if (organizationId is null)
        {
            if (!PlatformWide)
            {
                throw new InvalidOperationException(
                    $"Background job '{GetType().FullName}' was enqueued without an organization id. " +
                    "Tenant work must carry its organization in the job arguments; " +
                    "declare the job platform-wide only when it intentionally runs across organizations.");
            }

            await RunAsync(cancellationToken);
            return;
        }

        tenantContext.Set(organizationId.Value);
        try
        {
            using var activity = OrganizationTelemetry.StartActivity(ActivityName, organizationId.Value);
            await RunAsync(cancellationToken);
        }
        finally
        {
            tenantContext.Clear();
        }
    }

    /// <summary>Executes the job body under the tenant set by the base class.</summary>
    protected abstract Task RunAsync(CancellationToken cancellationToken);
}
