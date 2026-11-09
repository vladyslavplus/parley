using FluentValidation;
using Parley.Crm.Shared;
using Parley.SharedKernel.Errors;

namespace Parley.Crm.Features.Contacts.BulkAssignOwner;

public sealed class BulkAssignContactOwnerCommandValidator : AbstractValidator<BulkAssignContactOwnerCommand>
{
    public BulkAssignContactOwnerCommandValidator()
    {
        RuleFor(command => command.ContactIds)
            .NotEmpty().WithErrorCode(ErrorCodes.ValidationFailed)
            .Must(ids => ids.Count <= CrmLimits.BulkMaximumCount).WithErrorCode(ErrorCodes.ValidationFailed)
            .Must(ids => ids.All(id => id != Guid.Empty)).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(command => command.OwnerUserId)
            .NotEqual(Guid.Empty).WithErrorCode(ErrorCodes.ValidationFailed);
    }
}
