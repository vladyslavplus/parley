using System.Linq.Expressions;
using FluentValidation;
using Parley.Crm.Shared;
using Parley.SharedKernel.Errors;

namespace Parley.Crm.Features.Deals.Shared;

/// <summary>Validation rules shared by the deal create and update commands.</summary>
public static class DealRequestRules
{
    /// <summary>Applies the title, amount and currency rules to the properties of a deal command.</summary>
    public static void ApplyTo<TCommand>(
        AbstractValidator<TCommand> validator,
        Expression<Func<TCommand, string>> title,
        Expression<Func<TCommand, decimal>> amount,
        Expression<Func<TCommand, string>> currency)
    {
        validator.RuleFor(title)
            .Must(value => !string.IsNullOrWhiteSpace(value)).WithErrorCode(ErrorCodes.ValidationFailed)
            .MaximumLength(CrmLimits.DealTitleMaximumLength).WithErrorCode(ErrorCodes.ValidationFailed);
        validator.RuleFor(amount)
            .InclusiveBetween(0, CrmLimits.DealAmountMaximum).WithErrorCode(ErrorCodes.ValidationFailed);
        validator.RuleFor(currency)
            .Matches("^[A-Za-z]{3}$").WithErrorCode(ErrorCodes.ValidationFailed);
    }
}
