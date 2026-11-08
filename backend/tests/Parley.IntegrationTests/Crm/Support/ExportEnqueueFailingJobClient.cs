using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Parley.Crm.Exports;

namespace Parley.IntegrationTests.Crm.Support;

/// <summary>
/// Hangfire client that refuses only the contact export job, so the export request fails after its row
/// committed. Every other job, such as the registration email, goes to the real client.
/// </summary>
public sealed class ExportEnqueueFailingJobClient(IBackgroundJobClient inner) : IBackgroundJobClient
{
    private const string FailureMessage = "The job storage is unavailable.";

    public string Create(Job job, IState state) =>
        job.Type == typeof(ContactExportJob)
            ? throw new InvalidOperationException(FailureMessage)
            : inner.Create(job, state);

    public bool ChangeState(string jobId, IState state, string expectedState) => inner.ChangeState(jobId, state, expectedState);
}
