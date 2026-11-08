using System.Collections.Concurrent;

namespace Parley.IntegrationTests.Hangfire.Support;

/// <summary>
/// Captures the organizations observed by probe jobs.
/// </summary>
public sealed class ProbeRecorder
{
    private readonly ConcurrentBag<Guid> _seen = new();

    public IReadOnlyCollection<Guid> SeenOrganizationIds => _seen.ToArray();

    public void Record(Guid organizationId) => _seen.Add(organizationId);
}
