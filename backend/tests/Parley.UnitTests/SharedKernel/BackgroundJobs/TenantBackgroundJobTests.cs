using AwesomeAssertions;
using Parley.SharedKernel.BackgroundJobs;
using Parley.SharedKernel.Tenancy.Exceptions;
using Parley.SharedKernel.Tenancy.Implementations;
using Xunit;

namespace Parley.UnitTests.SharedKernel.BackgroundJobs;

public sealed class TenantBackgroundJobTests
{
    [Fact]
    public async Task ExecuteWithoutOrganization_FailsLoudlyUnlessPlatformWide()
    {
        var tenantContext = new JobTenantContext();
        var tenantJob = new TenantProbeJob(tenantContext, platformWide: false);
        var platformJob = new TenantProbeJob(tenantContext, platformWide: true);

        var tenantRun = async () => await tenantJob.ExecuteAsync(null, TestContext.Current.CancellationToken);
        await tenantRun.Should().ThrowAsync<InvalidOperationException>(
            "tenant work must never run without an organization");

        await platformJob.ExecuteAsync(null, TestContext.Current.CancellationToken);
        platformJob.PlatformWideRunCount.Should().Be(1,
            "an explicitly declared platform-wide job runs without an organization");
    }

    [Fact]
    public async Task ExecuteWithOrganization_RunsUnderThatTenantAndClearsItAfterwards()
    {
        var tenantContext = new JobTenantContext();
        var organizationId = Guid.CreateVersion7();
        var tenantJob = new TenantProbeJob(tenantContext, platformWide: false);

        await tenantJob.ExecuteAsync(organizationId, TestContext.Current.CancellationToken);

        tenantJob.ObservedOrganization.Should().Be(organizationId,
            "the job body runs under the tenant from its arguments");
        Action readAfter = () => _ = tenantContext.OrganizationId;
        readAfter.Should().Throw<TenantContextNotSetException>(
            "the ambient tenant is cleared after the job finishes");
    }

    [Fact]
    public async Task ExecuteWithOrganization_RunThrows_TenantIsCleared()
    {
        var tenantContext = new JobTenantContext();
        var organizationId = Guid.CreateVersion7();
        var tenantJob = new TenantProbeJob(tenantContext, platformWide: false, fail: true);

        var run = async () => await tenantJob.ExecuteAsync(organizationId, TestContext.Current.CancellationToken);
        await run.Should().ThrowAsync<ObjectDisposedException>();

        Action readAfter = () => _ = tenantContext.OrganizationId;
        readAfter.Should().Throw<TenantContextNotSetException>(
            "the ambient tenant is cleared even on failure");
    }

    private sealed class TenantProbeJob(JobTenantContext tenant, bool platformWide, bool fail = false)
        : TenantBackgroundJob(tenant)
    {
        protected override bool PlatformWide => platformWide;

        public Guid? ObservedOrganization { get; private set; }

        public int PlatformWideRunCount { get; private set; }

        protected override Task RunAsync(CancellationToken cancellationToken)
        {
            if (fail)
            {
                throw new ObjectDisposedException("probe", "probe failure");
            }

            PlatformWideRunCount++;
            try
            {
                ObservedOrganization = tenant.OrganizationId;
            }
            catch (TenantContextNotSetException)
            {
                ObservedOrganization = null;
            }

            return Task.CompletedTask;
        }
    }
}
