using System.Globalization;

namespace Parley.Identity.Features.Audit.Shared;

/// <summary>Sort field and instant parsing of the audit log list.</summary>
public static class AuditListRules
{
    public const string SortOccurredAt = "occurredAt";

    public static IReadOnlyList<string> SortFields { get; } = [SortOccurredAt];

    /// <summary>True when the value is absent or the sort field, which is the only one the log offers.</summary>
    public static bool IsValidSort(string? value) =>
        string.IsNullOrEmpty(value) || value == SortOccurredAt;

    /// <summary>
    /// Parses a UTC instant. A value without an offset is read as UTC. Returns null for an absent or
    /// unparseable value.
    /// </summary>
    public static DateTime? ParseInstant(string? value) =>
        !string.IsNullOrEmpty(value)
            && DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var instant)
                ? instant.UtcDateTime
                : null;

    /// <summary>True when the value is absent or a parseable instant.</summary>
    public static bool IsValidInstant(string? value) =>
        string.IsNullOrEmpty(value) || ParseInstant(value) is not null;
}
