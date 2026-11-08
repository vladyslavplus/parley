namespace Parley.Api.Controllers.Common;

/// <summary>Resolves the two-letter language of the current request, set from Accept-Language.</summary>
public static class RequestCulture
{
    public static string TwoLetter() => System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
}
