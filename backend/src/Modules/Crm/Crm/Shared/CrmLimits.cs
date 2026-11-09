namespace Parley.Crm.Shared;

/// <summary>
/// Input and column limits of the CRM module. Validators and column mappings read the same
/// values, so a request cannot pass validation and then overflow a column.
/// </summary>
public static class CrmLimits
{
    public const int PersonNameMaximumLength = 100;

    public const int EmailMaximumLength = 254;

    public const int PhoneMaximumLength = 32;

    public const int JobTitleMaximumLength = 100;

    public const int CityMaximumLength = 100;

    public const int CountryCodeLength = 2;

    public const int CompanyNameMaximumLength = 200;

    public const int DomainMaximumLength = 253;

    public const int IndustryMaximumLength = 100;

    public const int TagNameMaximumLength = 50;

    public const int TagColorMinimumIndex = 0;

    public const int TagColorMaximumIndex = 4;

    public const int SourceMaximumLength = 32;

    /// <summary>Source-side reference of a contact or an order (demo seed rows, imported ids).</summary>
    public const int ExternalRefMaximumLength = 64;

    /// <summary>Length of the order list search text (invoice number).</summary>
    public const int OrderSearchMaximumLength = 64;


    public const int SearchMaximumLength = 100;

    /// <summary>Shortest search term; shorter terms match too much to rank usefully.</summary>
    public const int SearchMinimumLength = 2;

    /// <summary>Most hits returned by one search request, across all kinds.</summary>
    public const int SearchResultMaximumCount = 20;

    /// <summary>Word similarity (pg_trgm) a record name needs to match a term that has no full-text hit. Tolerates a transposed or missing letter.</summary>
    public const double SearchTypoSimilarityThreshold = 0.3;

    public const int ExportStatusMaximumLength = 16;

    public const int ExportFileNameMaximumLength = 64;

    /// <summary>How long an export and its download link stay valid after the request.</summary>
    public static readonly TimeSpan ContactExportLinkLifetime = TimeSpan.FromHours(1);

    /// <summary>Expired exports removed per cleanup pass.</summary>
    public const int ContactExportCleanupBatchSize = 500;

    public const int BulkMaximumCount = 200;

    /// <summary>Owners returned by a lookup when the request gives no limit.</summary>
    public const int OwnerLookupDefaultLimit = 20;

    /// <summary>Most owners one lookup returns.</summary>
    public const int OwnerLookupMaximumLimit = 50;

    public const int CustomFieldKeyMaximumLength = 40;

    public const int CustomFieldEntityTypeMaximumLength = 32;

    public const int CustomFieldFieldTypeMaximumLength = 32;

    public const int CustomFieldLabelMaximumLength = 100;

    public const int CustomFieldOptionMaximumLength = 100;

    public const int CustomFieldOptionsMaximumCount = 50;

    public const int CustomFieldTextMaximumLength = 1000;

    public const int CustomFieldsPerEntityMaximumCount = 30;

    public const int PipelineNameMaximumLength = 100;

    public const int PipelineStageNameMaximumLength = 50;

    /// <summary>Longest resource key of a system-provided pipeline or stage name, such as the default stage names.</summary>
    public const int PipelineSystemKeyMaximumLength = 64;

    /// <summary>Most stages one pipeline holds, open and closed stages together.</summary>
    public const int PipelineStageMaximumCount = 12;

    /// <summary>Most open stages one pipeline holds: the maximum minus the won and lost stages.</summary>
    public const int PipelineOpenStageMaximumCount = PipelineStageMaximumCount - 2;

    public const int StageColorMinimumIndex = 0;

    public const int StageColorMaximumIndex = 4;

    public const int StageProbabilityMaximumBps = 10000;

    /// <summary>Position given to a stage while its final place is written in the same transaction; below every real position.</summary>
    public const int TemporaryStagePosition = -1_000_000;

    public const int DealTitleMaximumLength = 200;

    public const int DealLostReasonMaximumLength = 500;

    public const int CurrencyCodeLength = 3;

    public const int MoneyPrecision = 18;

    public const int MoneyScale = 2;

    /// <summary>Largest amount a deal can hold: the precision of the money column.</summary>
    public const decimal DealAmountMaximum = 9_999_999_999_999_999.99m;

    /// <summary>Distance between neighbouring board positions after a renumbering.</summary>
    public const long DealBoardPositionGap = 1024;

    public const int TaskTitleMaximumLength = 200;

    public const int TaskNotesMaximumLength = 2000;

    public const int NoteBodyMaximumLength = 4000;

    /// <summary>Tasks reminded per job pass for one organization.</summary>
    public const int TaskDueSoonBatchSize = 200;

    public const string EmptyJsonObject = "{}";

    public const string EmptyJsonArray = "[]";
}
