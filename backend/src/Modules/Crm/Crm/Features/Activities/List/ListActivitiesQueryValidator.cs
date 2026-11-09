using FluentValidation;
using Parley.Crm.Contracts.Errors;
using Parley.Crm.Domain.Activities;
using Parley.Crm.Shared;
using Parley.Crm.Shared.Subjects;
using Parley.SharedKernel.Errors;
using Parley.SharedKernel.Paging;

namespace Parley.Crm.Features.Activities.List;

public sealed class ListActivitiesQueryValidator : AbstractValidator<ListActivitiesQuery>
{
    public ListActivitiesQueryValidator()
    {
        RuleFor(query => query.After)
            .Must(value => value is null || KeysetCursor.TryDecode(value, out _)).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(query => query.Search)
            .MaximumLength(CrmLimits.SearchMaximumLength).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(query => query.Type)
            .Must(value => value is null || ActivityTypes.All.Contains(value)).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(query => query.Order)
            .Must(SortDirections.IsValid).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(query => query.ContactId)
            .Must((query, _) => SubjectChecks.CountOf(query.ContactId, query.DealId, query.CompanyId) > 0)
            .WithErrorCode(CrmErrorCodes.ActivitySubjectRequired);
        RuleFor(query => query.ContactId)
            .Must((query, _) => SubjectChecks.CountOf(query.ContactId, query.DealId, query.CompanyId) <= 1)
            .WithErrorCode(CrmErrorCodes.ActivitySubjectAmbiguous);
    }
}
