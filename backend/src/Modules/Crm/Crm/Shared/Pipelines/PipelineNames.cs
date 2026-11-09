using System.Globalization;
using Parley.SharedKernel.Localization;

namespace Parley.Crm.Shared.Pipelines;

/// <summary>
/// The one place that turns a stored pipeline or stage name into the name a reader sees. A system name (one with a
/// system key) is read from the shared texts in the request language; a name the user typed is shown as stored.
/// Call it after the query, never inside a projection: the texts cannot be translated to SQL.
/// </summary>
public static class PipelineNames
{
    public static string Display(string storedName, string? systemKey) =>
        systemKey is null
            ? storedName
            : SharedTexts.Get(systemKey, CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
}
