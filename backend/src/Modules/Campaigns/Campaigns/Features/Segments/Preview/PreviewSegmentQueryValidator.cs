using FluentValidation;
using Parley.Campaigns.Features.Segments.Shared;

namespace Parley.Campaigns.Features.Segments.Preview;

public sealed class PreviewSegmentQueryValidator : AbstractValidator<PreviewSegmentQuery>
{
    public PreviewSegmentQueryValidator()
    {
        RuleFor(query => query.Definition).SetValidator(new SegmentDefinitionValidator());
    }
}
