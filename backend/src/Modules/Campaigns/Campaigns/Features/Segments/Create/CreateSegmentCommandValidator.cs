using FluentValidation;
using Parley.Campaigns.Features.Segments.Shared;
using Parley.Campaigns.Shared;
using Parley.SharedKernel.Errors;

namespace Parley.Campaigns.Features.Segments.Create;

public sealed class CreateSegmentCommandValidator : AbstractValidator<CreateSegmentCommand>
{
    public CreateSegmentCommandValidator()
    {
        RuleFor(command => command.Name)
            .Must(value => !string.IsNullOrWhiteSpace(value)).WithErrorCode(ErrorCodes.ValidationFailed)
            .MaximumLength(CampaignsLimits.SegmentNameMaximumLength).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(command => command.Definition).SetValidator(new SegmentDefinitionValidator());
    }
}
