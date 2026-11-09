using FluentValidation;
using Parley.Crm.Shared;
using Parley.SharedKernel.Errors;

namespace Parley.Crm.Features.Pipelines.Rename;

public sealed class RenamePipelineCommandValidator : AbstractValidator<RenamePipelineCommand>
{
    public RenamePipelineCommandValidator()
    {
        RuleFor(command => command.Name)
            .Must(value => !string.IsNullOrWhiteSpace(value)).WithErrorCode(ErrorCodes.ValidationFailed)
            .MaximumLength(CrmLimits.PipelineNameMaximumLength).WithErrorCode(ErrorCodes.ValidationFailed);
    }
}
