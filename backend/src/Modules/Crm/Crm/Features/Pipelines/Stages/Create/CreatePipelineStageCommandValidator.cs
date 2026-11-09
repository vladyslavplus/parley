using FluentValidation;
using Parley.Crm.Shared;
using Parley.SharedKernel.Errors;

namespace Parley.Crm.Features.Pipelines.Stages.Create;

public sealed class CreatePipelineStageCommandValidator : AbstractValidator<CreatePipelineStageCommand>
{
    public CreatePipelineStageCommandValidator()
    {
        RuleFor(command => command.Name)
            .Must(value => !string.IsNullOrWhiteSpace(value)).WithErrorCode(ErrorCodes.ValidationFailed)
            .MaximumLength(CrmLimits.PipelineStageNameMaximumLength).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(command => command.ColorIndex)
            .InclusiveBetween(CrmLimits.StageColorMinimumIndex, CrmLimits.StageColorMaximumIndex).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(command => command.ProbabilityBps)
            .InclusiveBetween(0, CrmLimits.StageProbabilityMaximumBps).WithErrorCode(ErrorCodes.ValidationFailed);
    }
}
