using FluentValidation;
using Parley.Crm.Shared;
using Parley.SharedKernel.Errors;

namespace Parley.Crm.Features.Tags.Update;

public sealed class UpdateTagCommandValidator : AbstractValidator<UpdateTagCommand>
{
    public UpdateTagCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEqual(Guid.Empty).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(command => command.Name)
            .Must(value => !string.IsNullOrWhiteSpace(value)).WithErrorCode(ErrorCodes.ValidationFailed)
            .MaximumLength(CrmLimits.TagNameMaximumLength).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(command => command.ColorIndex)
            .Must(value => value is >= CrmLimits.TagColorMinimumIndex and <= CrmLimits.TagColorMaximumIndex)
            .WithErrorCode(ErrorCodes.ValidationFailed);
    }
}
