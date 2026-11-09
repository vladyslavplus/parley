using System.Linq.Expressions;
using FluentValidation;
using Parley.Crm.Shared;
using Parley.SharedKernel.Errors;

namespace Parley.Crm.Features.Tasks.Shared;

/// <summary>Validation rules shared by the task create and update commands.</summary>
public static class TaskRequestRules
{
    /// <summary>Applies the title and notes rules to the properties of a task command.</summary>
    public static void ApplyTo<TCommand>(
        AbstractValidator<TCommand> validator,
        Expression<Func<TCommand, string>> title,
        Expression<Func<TCommand, string?>> notes)
    {
        validator.RuleFor(title)
            .Must(value => !string.IsNullOrWhiteSpace(value)).WithErrorCode(ErrorCodes.ValidationFailed)
            .MaximumLength(CrmLimits.TaskTitleMaximumLength).WithErrorCode(ErrorCodes.ValidationFailed);
        validator.RuleFor(notes)
            .MaximumLength(CrmLimits.TaskNotesMaximumLength).WithErrorCode(ErrorCodes.ValidationFailed);
    }
}
