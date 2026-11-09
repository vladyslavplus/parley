using FluentValidation;
using Parley.Crm.Shared;
using Parley.SharedKernel.Errors;

namespace Parley.Crm.Features.Contacts.Tags;

public sealed class AttachContactTagsCommandValidator : AbstractValidator<AttachContactTagsCommand>
{
    public AttachContactTagsCommandValidator()
    {
        RuleFor(command => command.ContactId)
            .NotEqual(Guid.Empty).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(command => command.TagIds)
            .NotEmpty().WithErrorCode(ErrorCodes.ValidationFailed)
            .Must(ids => ids.Count <= CrmLimits.BulkMaximumCount).WithErrorCode(ErrorCodes.ValidationFailed)
            .Must(ids => ids.All(id => id != Guid.Empty)).WithErrorCode(ErrorCodes.ValidationFailed);
    }
}
