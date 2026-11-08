using FluentValidation;
using Parley.Identity.Domain.Users;
using Parley.SharedKernel.Errors;
using Parley.SharedKernel.Localization;

namespace Parley.Identity.Shared.Validation;

/// <summary>
/// Field rules for organization settings and profile values. Each failure carries
/// <see cref="ErrorCodes.ValidationFailed"/>, the shared localized field message.
/// </summary>
public static class IdentityFieldRules
{
    public static IRuleBuilderOptions<T, string> TimeZone<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithErrorCode(ErrorCodes.ValidationFailed)
            .MaximumLength(IdentityLimits.TimeZoneMaximumLength).WithErrorCode(ErrorCodes.ValidationFailed)
            .Must(IanaTimeZones.IsKnown).WithErrorCode(ErrorCodes.ValidationFailed);

    public static IRuleBuilderOptions<T, string> Currency<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithErrorCode(ErrorCodes.ValidationFailed)
            .Must(IsCurrencyCode).WithErrorCode(ErrorCodes.ValidationFailed);

    public static IRuleBuilderOptions<T, string> SupportedLocale<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithErrorCode(ErrorCodes.ValidationFailed)
            .Must(SupportedLocales.Contains).WithErrorCode(ErrorCodes.ValidationFailed);

    public static IRuleBuilderOptions<T, string> Theme<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithErrorCode(ErrorCodes.ValidationFailed)
            .Must(UserThemes.All.Contains).WithErrorCode(ErrorCodes.ValidationFailed);

    public static IRuleBuilderOptions<T, string?> DisplayName<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(IdentityLimits.DisplayNameMaximumLength).WithErrorCode(ErrorCodes.ValidationFailed);

    public static IRuleBuilderOptions<T, string> OrganizationName<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithErrorCode(ErrorCodes.ValidationFailed)
            .MaximumLength(IdentityLimits.OrganizationNameMaximumLength).WithErrorCode(ErrorCodes.ValidationFailed);

    private static readonly IReadOnlyList<string> SupportedLocales = [CultureCodes.Ukrainian, CultureCodes.English];

    /// <summary>Three uppercase ASCII letters, the shape of an ISO 4217 code.</summary>
    private static bool IsCurrencyCode(string value) =>
        value.Length == IdentityLimits.CurrencyCodeLength && value.All(character => character is >= 'A' and <= 'Z');
}
