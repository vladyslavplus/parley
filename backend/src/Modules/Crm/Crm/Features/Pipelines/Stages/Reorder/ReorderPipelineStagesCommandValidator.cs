using FluentValidation;
using Parley.SharedKernel.Errors;

namespace Parley.Crm.Features.Pipelines.Stages.Reorder;

public sealed class ReorderPipelineStagesCommandValidator : AbstractValidator<ReorderPipelineStagesCommand>
{
    public ReorderPipelineStagesCommandValidator()
    {
        RuleFor(command => command.StageIds)
            .NotNull().WithErrorCode(ErrorCodes.ValidationFailed)
            .NotEmpty().WithErrorCode(ErrorCodes.ValidationFailed);
    }
}
