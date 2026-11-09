using Parley.Crm.Domain.Deals;
using Parley.Crm.Domain.Pipelines;
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

    public const string StageNameNormalized = "lower(\"name\")";

    public const string DealTitleNormalized = "lower(\"title\")";

    public const string DealSearchVector = "to_tsvector('simple'::regconfig, coalesce(\"title\", ''))";

    public const string TaskTitleNormalized = "lower(\"title\")";

    public const string NoteSubjectCheck = "num_nonnulls(\"contact_id\", \"deal_id\", \"company_id\") = 1";

    /// <summary>Tasks still open and not yet reminded: the rows the due-soon sweep reads.</summary>
    public const string OpenUnremindedTaskFilter = "\"completed_at\" IS NULL AND \"deleted_at\" IS NULL AND \"reminder_sent_at\" IS NULL";

    public const string DefaultPipelineFilter = "\"is_default\"";

    public static string StageKindCheck { get; } =
        $"\"kind\" IN ('{PipelineStageKinds.Open}', '{PipelineStageKinds.Won}', '{PipelineStageKinds.Lost}')";

    public static string StageColorCheck { get; } =
        $"\"color_index\" BETWEEN {CrmLimits.StageColorMinimumIndex} AND {CrmLimits.StageColorMaximumIndex}";

    public static string StageProbabilityCheck { get; } =
        $"\"probability_bps\" BETWEEN 0 AND {CrmLimits.StageProbabilityMaximumBps}";

    public static string DealStatusCheck { get; } =
        $"\"status\" IN ('{DealStatuses.Open}', '{DealStatuses.Won}', '{DealStatuses.Lost}')";

    public const string LiveRowFilter = "\"deleted_at\" IS NULL";

    public const string ExternalRefPresentFilter = "\"external_ref\" IS NOT NULL";

    public static string TagColorCheck { get; } =
        $"\"color_index\" BETWEEN {CrmLimits.TagColorMinimumIndex} AND {CrmLimits.TagColorMaximumIndex}";
}
