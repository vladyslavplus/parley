using FluentValidation;
using Parley.Crm.Shared;
using Parley.SharedKernel.Errors;

namespace Parley.Crm.Features.Owners.List;

public sealed class ListOwnersQueryValidator : AbstractValidator<ListOwnersQuery>
{
    public ListOwnersQueryValidator()
    {
        RuleFor(query => query.Q)
            .MaximumLength(CrmLimits.SearchMaximumLength).WithErrorCode(ErrorCodes.ValidationFailed);
    }
}
