using FluentValidation;
using Parley.Crm.Domain.CustomFields;
using Parley.Crm.Shared;
using Parley.Crm.Shared.CustomFields;
using Parley.SharedKernel.Errors;

namespace Parley.Crm.Features.CustomFields.Create;

public sealed class CreateCustomFieldCommandValidator : AbstractValidator<CreateCustomFieldCommand>
{
    public CreateCustomFieldCommandValidator()
    {
        RuleFor(command => command.EntityType)
            .Must(CustomFieldRules.IsValidEntityType).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(command => command.Key)
            .NotEmpty().WithErrorCode(ErrorCodes.ValidationFailed)
            .MaximumLength(CrmLimits.CustomFieldKeyMaximumLength).WithErrorCode(ErrorCodes.ValidationFailed)
            .Matches(CustomFieldRules.KeyPattern).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(command => command.Label)
            .Must(value => !string.IsNullOrWhiteSpace(value)).WithErrorCode(ErrorCodes.ValidationFailed)
            .MaximumLength(CrmLimits.CustomFieldLabelMaximumLength).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(command => command.FieldType)
            .Must(CustomFieldRules.IsValidFieldType).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(command => command.Options)
            .Must(options => options is null || options.Count <= CrmLimits.CustomFieldOptionsMaximumCount).WithErrorCode(ErrorCodes.ValidationFailed)
            .Must(options => options is null || options.All(option => !string.IsNullOrWhiteSpace(option) && option.Length <= CrmLimits.CustomFieldOptionMaximumLength))
            .WithErrorCode(ErrorCodes.ValidationFailed)
            .Must(options => options is null || options.Distinct(StringComparer.Ordinal).Count() == options.Count).WithErrorCode(ErrorCodes.ValidationFailed)
            .Must((command, options) => command.FieldType != CustomFieldTypes.Select || (options is { Count: > 0 }))
            .WithErrorCode(ErrorCodes.ValidationFailed)
            .Must((command, options) => command.FieldType == CustomFieldTypes.Select || options is null or { Count: 0 })
            .WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(command => command.Position)
            .InclusiveBetween(0, CrmLimits.CustomFieldsPerEntityMaximumCount).WithErrorCode(ErrorCodes.ValidationFailed);
    }
}
