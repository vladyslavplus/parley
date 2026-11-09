using FluentValidation;
using Parley.Crm.Contracts.Errors;
using Parley.Crm.Shared;
using Parley.Crm.Shared.Subjects;
using Parley.SharedKernel.Errors;

namespace Parley.Crm.Features.Notes.Create;

public sealed class CreateNoteCommandValidator : AbstractValidator<CreateNoteCommand>
{
    public CreateNoteCommandValidator()
    {
        RuleFor(command => command.Body)
            .Must(value => !string.IsNullOrWhiteSpace(value)).WithErrorCode(ErrorCodes.ValidationFailed)
            .MaximumLength(CrmLimits.NoteBodyMaximumLength).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(command => command.ContactId)
            .Must((command, _) => SubjectChecks.CountOf(command.ContactId, command.DealId, command.CompanyId) == 1)
            .WithErrorCode(CrmErrorCodes.NoteSubjectRequired);
    }
}
