using Parley.Crm.Shared;

namespace Parley.Crm.Persistence;

/// <summary>
/// SQL fragments of generated columns and partial-index filters. Every fragment is immutable
/// (required by stored generated columns) and refers to columns by their mapped names.
/// </summary>
public static class CrmSql
{
    public const string ContactNameNormalized = "lower(\"first_name\" || ' ' || coalesce(\"last_name\", ''))";

    public const string ContactSearchVector =
        "to_tsvector('simple'::regconfig, coalesce(\"first_name\", '') || ' ' || coalesce(\"last_name\", '') || ' ' || coalesce(\"email\", '') || ' ' || coalesce(\"phone\", ''))";

    public const string CompanyNameNormalized = "lower(\"name\")";

    public const string CompanySearchVector =
        "to_tsvector('simple'::regconfig, coalesce(\"name\", '') || ' ' || coalesce(\"domain\", ''))";

    public const string TagNameNormalized = "lower(\"name\")";

    public const string LiveRowFilter = "\"deleted_at\" IS NULL";

    public static string TagColorCheck { get; } =
        $"\"color_index\" BETWEEN {CrmLimits.TagColorMinimumIndex} AND {CrmLimits.TagColorMaximumIndex}";
}
