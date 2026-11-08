using System.Globalization;
using Microsoft.Extensions.Localization;
using Parley.SharedKernel.Errors;
using Parley.SharedKernel.Results;

namespace Parley.Api.Localization;

/// <summary>
/// Resolves error codes to localized texts from the Accept-Language culture
/// or from an explicit culture for background work.
/// </summary>
public sealed class ErrorTextLocalizer(IStringLocalizerFactory localizerFactory)
{
    public string Get(Error error) => Get(error.Code, error.Parameters);

    public string Get(Error error, CultureInfo culture) => Get(error.Code, error.Parameters, culture);

    public string Get(FieldError fieldError) => Get(fieldError.Code, fieldError.Parameters);

    public string Get(FieldError fieldError, CultureInfo culture) => Get(fieldError.Code, fieldError.Parameters, culture);

    private string Get(string code, IReadOnlyDictionary<string, string>? parameters) =>
        Get(code, parameters, CultureInfo.CurrentCulture);

    private string Get(string code, IReadOnlyDictionary<string, string>? parameters, CultureInfo culture)
    {
        var localizer = localizerFactory.Create(ErrorTextResources.BaseName, ErrorTextResources.AssemblyName);
        var args = parameters?.Values.Cast<object>().ToArray() ?? [];
        var previousCulture = CultureInfo.CurrentCulture;
        var previousUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            var localized = localizer[code, args];
            return localized.ResourceNotFound ? code : localized.Value;
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }
}
