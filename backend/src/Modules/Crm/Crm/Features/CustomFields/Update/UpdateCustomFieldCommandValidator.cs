using FluentValidation;
using Parley.Crm.Shared;
using Parley.SharedKernel.Errors;

namespace Parley.Crm.Features.CustomFields.Update;

public sealed class UpdateCustomFieldCommandValidator : AbstractValidator<UpdateCustomFieldCommand>
{
    public UpdateCustomFieldCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEqual(Guid.Empty).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(command => command.Label)
            .Must(value => !string.IsNullOrWhiteSpace(value)).WithErrorCode(ErrorCodes.ValidationFailed)
            .MaximumLength(CrmLimits.CustomFieldLabelMaximumLength).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(command => command.Options)
            .Must(options => options is null || options.Count <= CrmLimits.CustomFieldOptionsMaximumCount).WithErrorCode(ErrorCodes.ValidationFailed)
            .Must(options => options is null || options.All(option => !string.IsNullOrWhiteSpace(option) && option.Length <= CrmLimits.CustomFieldOptionMaximumLength))
            .WithErrorCode(ErrorCodes.ValidationFailed)
            .Must(options => options is null || options.Distinct(StringComparer.Ordinal).Count() == options.Count).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(command => command.Position)
            .InclusiveBetween(0, CrmLimits.CustomFieldsPerEntityMaximumCount).WithErrorCode(ErrorCodes.ValidationFailed);
    }
}
