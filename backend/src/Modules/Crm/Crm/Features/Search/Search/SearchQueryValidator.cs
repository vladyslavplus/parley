using FluentValidation;
using Parley.Crm.Contracts.Errors;
using Parley.Crm.Shared;

namespace Parley.Crm.Features.Search.Search;

public sealed class SearchQueryValidator : AbstractValidator<SearchQuery>
{
    public SearchQueryValidator()
    {
        RuleFor(query => query.Q)
            .Must(value => value is not null && value.Trim().Length >= CrmLimits.SearchMinimumLength)
            .WithErrorCode(CrmErrorCodes.SearchQueryTooShort);
        RuleFor(query => query.Q)
            .Must(value => value is null || value.Trim().Length <= CrmLimits.SearchMaximumLength)
            .WithErrorCode(CrmErrorCodes.SearchQueryTooLong);
    }
}
