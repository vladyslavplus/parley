using System.Globalization;
using System.Resources;
using Parley.SharedKernel.Errors;

namespace Parley.SharedKernel.Localization;

/// <summary>
/// Reads shared texts by key for code that runs without an HTTP request, such as an integration
/// event consumer that seeds rows in the organization's locale. Keys and values live in the
/// shared resource files next to the error texts.
/// </summary>
public static class SharedTexts
{
    private static readonly ResourceManager Resources = new(ErrorTextResources.BaseName, typeof(SharedTexts).Assembly);

    /// <summary>Returns the text of <paramref name="key"/> in <paramref name="culture"/>, or the key when it has no entry.</summary>
    public static string Get(string key, string culture) =>
        Resources.GetString(key, CultureInfo.GetCultureInfo(culture)) ?? key;
}
