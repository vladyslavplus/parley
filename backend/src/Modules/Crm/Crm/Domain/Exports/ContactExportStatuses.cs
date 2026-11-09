namespace Parley.Crm.Domain.Exports;

/// <summary>Lifecycle states of a contact CSV export.</summary>
public static class ContactExportStatuses
{
    public const string Queued = "queued";

    public const string Running = "running";

    public const string Completed = "completed";

    public const string Failed = "failed";

    public static IReadOnlyList<string> All { get; } = [Queued, Running, Completed, Failed];
}
