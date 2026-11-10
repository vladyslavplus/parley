using FluentValidation;
using Parley.Campaigns.Features.Segments.Shared;
using Parley.Campaigns.Shared;
using Parley.SharedKernel.Errors;

namespace Parley.Campaigns.Features.Segments.Update;

public sealed class UpdateSegmentCommandValidator : AbstractValidator<UpdateSegmentCommand>
{
    public UpdateSegmentCommandValidator()
    {
        RuleFor(command => command.Name)
            .Must(value => !string.IsNullOrWhiteSpace(value)).WithErrorCode(ErrorCodes.ValidationFailed)
            .MaximumLength(CampaignsLimits.SegmentNameMaximumLength).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(command => command.Definition).SetValidator(new SegmentDefinitionValidator());
        RuleFor(command => command.Version)
            .GreaterThan(0u).WithErrorCode(ErrorCodes.ValidationFailed);
    }
}
