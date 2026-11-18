using FluentValidation;
using Parley.Crm.Domain.Tasks;
using Parley.Crm.Shared;
using Parley.SharedKernel.Errors;

namespace Parley.Crm.Features.Tasks.OverdueCount;

public sealed class CountOverdueTasksQueryValidator : AbstractValidator<CountOverdueTasksQuery>
{
    public CountOverdueTasksQueryValidator()
    {
        RuleFor(query => query.Search)
            .MaximumLength(CrmLimits.SearchMaximumLength).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(query => query.Type)
            .Must(value => value is null || TaskTypes.IsValid(value)).WithErrorCode(ErrorCodes.ValidationFailed);
    }
}
