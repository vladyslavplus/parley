using System.Diagnostics;

namespace Parley.SharedKernel.Observability;

/// <summary>
/// Trace source for organization-scoped background work. The outbox dispatcher
/// and Hangfire job base start an activity per unit of work and tag it with
/// the tenant organization id (id only, no personal data). The host enables
/// this source for the OpenTelemetry exporter so the tag reaches the Aspire
/// Dashboard alongside the logs.
/// </summary>
public static class OrganizationTelemetry
{
    public const string ActivitySourceName = "parley.organization";

    public const string OrganizationIdTag = "organization_id";

    private static readonly ActivitySource Source = new(ActivitySourceName);

    /// <summary>
    /// Starts an activity for one unit of work under the given organization.
    /// The organization id travels as a trace tag: an opaque identifier, never
    /// personal data.
    /// </summary>
    public static Activity? StartActivity(string name, Guid organizationId)
    {
        var activity = Source.StartActivity(name);
        activity?.SetTag(OrganizationIdTag, organizationId.ToString());
        return activity;
    }
}
