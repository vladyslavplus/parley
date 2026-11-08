using System.Text.RegularExpressions;

namespace Parley.Crm.Exports;

/// <summary>
/// Names of the export files. The name is derived from the export identifier, so a request
/// can never choose a path, and <see cref="IsValidFileName"/> guards every read from storage.
/// </summary>
public static partial class ContactExportFiles
{
    private const string Extension = ".csv";

    /// <summary>File name the browser receives on download.</summary>
    public const string DownloadName = "contacts-export.csv";

    public static string FileNameFor(Guid exportId) => $"{exportId:N}{Extension}";

    public static bool IsValidFileName(string? fileName) => fileName is not null && FileNamePattern().IsMatch(fileName);

    [GeneratedRegex(@"^[0-9a-f]{32}\.csv$", RegexOptions.CultureInvariant)]
    private static partial Regex FileNamePattern();
}
