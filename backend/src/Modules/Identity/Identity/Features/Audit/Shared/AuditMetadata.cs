namespace Parley.Identity.Features.Audit.Shared;

/// <summary>Stored metadata of an audit row. Metadata stays empty so no personal payload can be written.</summary>
public static class AuditMetadata
{
    public const string Empty = "{}";
}
