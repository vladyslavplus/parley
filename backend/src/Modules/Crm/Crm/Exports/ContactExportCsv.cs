using System.Globalization;

namespace Parley.Crm.Exports;

/// <summary>Header and row layout of the contact export file.</summary>
public static class ContactExportCsv
{
    public const string Header = "id,first_name,last_name,email,phone,job_title,country,city,company_id,owner_user_id,created_at";

    private const char Separator = ',';

    public static string FormatRow(ContactExportRow row) => string.Join(Separator,
        CsvCell.Format(row.Id.ToString("D", CultureInfo.InvariantCulture)),
        CsvCell.Format(row.FirstName),
        CsvCell.Format(row.LastName),
        CsvCell.Format(row.Email),
        CsvCell.Format(row.Phone),
        CsvCell.Format(row.JobTitle),
        CsvCell.Format(row.Country),
        CsvCell.Format(row.City),
        CsvCell.Format(row.CompanyId?.ToString("D", CultureInfo.InvariantCulture)),
        CsvCell.Format(row.OwnerUserId?.ToString("D", CultureInfo.InvariantCulture)),
        CsvCell.Format(row.CreatedAt.ToString("O", CultureInfo.InvariantCulture)));
}
