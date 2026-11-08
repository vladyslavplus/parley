using FluentValidation;
using Parley.Crm.Contracts.Errors;
using Parley.Crm.Shared;
using Parley.SharedKernel.Errors;

namespace Parley.Crm.Features.Contacts.Exports.Request;

public sealed class RequestContactExportCommandValidator : AbstractValidator<RequestContactExportCommand>
{
    public RequestContactExportCommandValidator()
    {
        RuleFor(command => command.Filter.Search)
            .MaximumLength(CrmLimits.SearchMaximumLength).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(command => command.Filter)
            .Must(filter => filter.CreatedFrom is null || filter.CreatedTo is null || filter.CreatedFrom <= filter.CreatedTo)
            .WithErrorCode(CrmErrorCodes.ContactExportRangeInvalid);
    }
}
