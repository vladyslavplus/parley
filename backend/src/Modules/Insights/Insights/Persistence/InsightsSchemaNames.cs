namespace Parley.Insights.Persistence;

/// <summary>Schema, table, index and extension names of the insights module. Mappings and migrations read the same values.</summary>
public static class InsightsSchemaNames
{
    public const string Schema = "insights";

    public const string VectorExtension = "vector";

    public const string DailyRunsTable = "daily_runs";

    public const string RfmSnapshotsTable = "rfm_snapshots";

    public const string PriorityItemsTable = "priority_items";

    public const string MlSettingsTable = "ml_settings";

    public const string ModelVersionsTable = "model_versions";

    public const string ScoringRunsTable = "scoring_runs";

    public const string ScoresTable = "scores";

    public const string LatestScoresTable = "latest_scores";

    public const string DailyRunDayIndex = "ux_daily_runs_organization_id_as_of";

    public const string RfmSnapshotContactDayIndex = "ux_rfm_snapshots_organization_id_contact_id_as_of";

    public const string RfmSnapshotDaySegmentIndex = "ix_rfm_snapshots_organization_id_as_of_segment";

    public const string PriorityItemContactDayIndex = "ux_priority_items_organization_id_as_of_contact_id";

    public const string PriorityItemOwnerDayIndex = "ix_priority_items_organization_id_as_of_owner_user_id";

    public const string MlSettingOrganizationIndex = "ux_ml_settings_organization_id";

    public const string ModelVersionNameIndex = "ux_model_versions_model_name_version";

    public const string ScoreRunContactKindIndex = "ux_scores_scoring_run_id_contact_id_score_kind";
}
