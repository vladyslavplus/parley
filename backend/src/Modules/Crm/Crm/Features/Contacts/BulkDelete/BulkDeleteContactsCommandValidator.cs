using FluentValidation;
using Parley.Crm.Shared;
using Parley.SharedKernel.Errors;

namespace Parley.Crm.Features.Contacts.BulkDelete;

public sealed class BulkDeleteContactsCommandValidator : AbstractValidator<BulkDeleteContactsCommand>
{
    public BulkDeleteContactsCommandValidator()
    {
        RuleFor(command => command.ContactIds)
            .NotEmpty().WithErrorCode(ErrorCodes.ValidationFailed)
            .Must(ids => ids.Count <= CrmLimits.BulkMaximumCount).WithErrorCode(ErrorCodes.ValidationFailed)
            .Must(ids => ids.All(id => id != Guid.Empty)).WithErrorCode(ErrorCodes.ValidationFailed);
    }
}
