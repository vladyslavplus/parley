using System.Diagnostics;
using AwesomeAssertions;
using Parley.SharedKernel.Observability;
using Xunit;

namespace Parley.UnitTests.SharedKernel.Observability;

public sealed class OrganizationTelemetryTests
{
    [Fact]
    public void StartActivity_TagsOrganizationId()
    {
        var organizationId = Guid.CreateVersion7();
        Activity? captured = null;

        using var listener = new ActivityListener();
        listener.ShouldListenTo = source => source.Name == OrganizationTelemetry.ActivitySourceName;
        listener.Sample = (ref _) => ActivitySamplingResult.AllData;
        listener.ActivityStarted = activity => captured = activity;
        ActivitySource.AddActivityListener(listener);

        using var activity = OrganizationTelemetry.StartActivity("test.operation", organizationId);

        captured.Should().NotBeNull("the activity is emitted through the shared source");
        captured!.Tags.Should().ContainSingle(
            tag => tag.Key == OrganizationTelemetry.OrganizationIdTag && tag.Value == organizationId.ToString(),
            "the organization id travels as a trace tag (id only)");
    }
}
