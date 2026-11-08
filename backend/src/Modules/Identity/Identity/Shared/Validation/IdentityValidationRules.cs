using FluentValidation;
using Parley.Identity.Options.Auth;
using Parley.SharedKernel.Errors;

namespace Parley.Identity.Shared.Validation;

/// <summary>
/// Shared field rules. Every failure carries <see cref="ErrorCodes.ValidationFailed"/>
/// so the field message comes from one localized text.
/// </summary>
public static class IdentityValidationRules
{
    public static IRuleBuilderOptions<T, string> Email<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithErrorCode(ErrorCodes.ValidationFailed)
            .MaximumLength(IdentityLimits.EmailMaximumLength).WithErrorCode(ErrorCodes.ValidationFailed)
            .EmailAddress().WithErrorCode(ErrorCodes.ValidationFailed);

    public static IRuleBuilderOptions<T, string> Token<T>(this IRuleBuilder<T, string> rule, int maximumLength) =>
        rule.NotEmpty().WithErrorCode(ErrorCodes.ValidationFailed)
            .MaximumLength(maximumLength).WithErrorCode(ErrorCodes.ValidationFailed);

    public static IRuleBuilderOptions<T, string> Password<T>(this IRuleBuilder<T, string> rule, int minimumLength) =>
        rule.NotEmpty().WithErrorCode(ErrorCodes.ValidationFailed)
            .MinimumLength(minimumLength).WithErrorCode(ErrorCodes.ValidationFailed)
            .MaximumLength(AuthDefaults.MaximumPasswordLength).WithErrorCode(ErrorCodes.ValidationFailed);
}
