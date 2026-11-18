using FluentValidation;
using Parley.Crm.Domain.Deals;
using Parley.Crm.Shared;
using Parley.SharedKernel.Errors;

namespace Parley.Crm.Features.Pipelines.Board;

public sealed class GetPipelineBoardQueryValidator : AbstractValidator<GetPipelineBoardQuery>
{
    public GetPipelineBoardQueryValidator()
    {
        RuleFor(query => query.Search)
            .MaximumLength(CrmLimits.SearchMaximumLength).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(query => query.Status)
            .Must(value => value is null || DealStatuses.All.Contains(value))
            .WithErrorCode(ErrorCodes.ValidationFailed);
    }
}
