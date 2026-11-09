using FluentValidation;
using Parley.Crm.Contracts.Errors;
using Parley.Crm.Shared;
using Parley.Crm.Shared.Subjects;
using Parley.SharedKernel.Errors;
using Parley.SharedKernel.Paging;

namespace Parley.Crm.Features.Notes.List;

public sealed class ListNotesQueryValidator : AbstractValidator<ListNotesQuery>
{
    public ListNotesQueryValidator()
    {
        RuleFor(query => query.After)
            .Must(value => value is null || KeysetCursor.TryDecode(value, out _)).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(query => query.Search)
            .MaximumLength(CrmLimits.SearchMaximumLength).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(query => query.Order)
            .Must(SortDirections.IsValid).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(query => query.ContactId)
            .Must((query, _) => SubjectChecks.CountOf(query.ContactId, query.DealId, query.CompanyId) == 1)
            .WithErrorCode(CrmErrorCodes.NoteSubjectRequired);
    }
}
